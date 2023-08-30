import json
import numpy as np
import onnxruntime as ort
import tensorflow as tf
from sklearn.model_selection import train_test_split
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import Conv1D, MaxPooling1D, Flatten, Dense, Dropout, BatchNormalization
from tensorflow.keras.regularizers import l2
from tensorflow.keras.callbacks import EarlyStopping
from sklearn.utils.class_weight import compute_class_weight
import subprocess


# Read data from JSON file
with open('Fireball.json', 'r') as file:
    data = json.load(file)

# Define the number of frames
num_frames = 10

# Extract sequences
sequences = []
labels = []
first_motion_done = False  # flag to indicate if the first motion has been processed

for motion in data['Motions']:
    #if first_motion_done:
        #break  # Skip the remaining motions after the first one is processed
    
    # Start from num_frames and go to the last frame
    for i in range(num_frames, len(motion['Frames'])):
        sequence = [info['FrameInfo'] + [info['Time']] for info in motion['Frames'][i-num_frames:i]]
        
        # Build debug string
        debug_str = f"frame: {i} "
        for frame_index, frame in enumerate(sequence):
            for input_index, input_value in enumerate(frame):
                debug_str += f"'Inputs[{input_index + frame_index * len(frame)}]':{input_value:.3f} "
        
        # Log indices of frames involved in this sequence
        frame_indices = [str(index) for index in range(i-num_frames, i)]
        #print(f"Indices of frames involved: {','.join(frame_indices)}")
        if(i == num_frames & first_motion_done == False):
            first_motion_done = True
            print(debug_str)  # Debugging statement
        
        sequences.append(sequence)
        
        label = motion['Frames'][i - 1]['Active']
        labels.append(int(label))
            
    first_motion_done = True  # Mark that the first motion has been processed

# Convert to numpy arrays
X = np.array(sequences, dtype='float32')
y = np.array(labels, dtype='float32')

# Compute class weights
class_weights = compute_class_weight('balanced', classes=np.unique(y), y=y)
class_weight_dict = {i: weight for i, weight in enumerate(class_weights)}

# Split data into training and validation sets
X_train, X_val, y_train, y_val = train_test_split(X, y, test_size=0.2, shuffle=True)

# Define CNN model
model = Sequential([
    Conv1D(256, 3, activation='relu', kernel_regularizer=l2(0.001), input_shape=(num_frames, X_train.shape[2])),
    BatchNormalization(),
    MaxPooling1D(2),
    Dropout(0.5),
    Conv1D(512, 3, activation='relu', kernel_regularizer=l2(0.001)),
    BatchNormalization(),
    MaxPooling1D(2),
    Dropout(0.5),
    Flatten(),
    Dense(256, activation='relu', kernel_regularizer=l2(0.001)),
    Dropout(0.5),
    Dense(128, activation='relu', kernel_regularizer=l2(0.001)),
    Dropout(0.5),
    Dense(1, activation='sigmoid')
])

# Compile the model
model.compile(optimizer='adam', loss='binary_crossentropy', metrics=['accuracy'])

# Early stopping callback
early_stopping = EarlyStopping(monitor='val_loss', patience=5, restore_best_weights=True)

# Train the model with class weights
model.fit(X_train, y_train, epochs=50, validation_data=(X_val, y_val), callbacks=[early_stopping], class_weight=class_weight_dict)

# Initialize counters
correct_true_guess = 0
correct_false_guess = 0
incorrect_true_guess = 0
incorrect_false_guess = 0

# Evaluate the model on the validation set
y_pred = model.predict(X_val).squeeze()
y_pred_bool = y_pred > 0.5

# Iterate through true and predicted labels
for true_label, predicted_label in zip(y_val, y_pred_bool):
    if true_label == 1:
        if predicted_label == 1:
            correct_true_guess += 1
        else:
            incorrect_false_guess += 1
    else:
        if predicted_label == 0:
            correct_false_guess += 1
        else:
            incorrect_true_guess += 1

# Output counts
print(f"Correct True Guess: {correct_true_guess}")
print(f"Correct False Guess: {correct_false_guess}")
print(f"Incorrect True Guess: {incorrect_true_guess}")
print(f"Incorrect False Guess: {incorrect_false_guess}")

# Calculate accuracy
total = correct_true_guess + correct_false_guess + incorrect_true_guess + incorrect_false_guess
accuracy = (correct_true_guess + correct_false_guess) / total
print(f"Model Accuracy: {accuracy * 100:.2f}%")

# Save the model
model_path = 'saved_model'
model.save(model_path)

try:
    # Convert to ONNX
    onnx_output_directory = "B:\\GitProjects\\NewMachineLearning\\NewMachineLearning\\Assets\\Scripts\\PyTest"
    onnx_command = f"python -m tf2onnx.convert --saved-model saved_model --output {onnx_output_directory}\\model.onnx"
    #onnx_command = f"python -m tf2onnx.convert --saved-model saved_model --output {onnx_output_directory}/model.onnx --fold_const False"
    #onnx_command = f"python -m tf2onnx.convert --saved-model saved_model --output {onnx_output_directory}\\model.onnx --optimizers ''"
    #onnx_command = f"python -m tf2onnx.convert --saved-model saved_model --output {onnx_output_directory}\\model.onnx --optimization disable"
    
    process = subprocess.run(onnx_command, shell=True, check=True)

    if process.returncode == 0:
        print(f"Conversion successful. ONNX model is saved in {onnx_output_directory}.")
    else:
        print("Conversion failed. Please check the command and environment.")

except subprocess.CalledProcessError as e:
    print("Subprocess failed with the following error:")
    print(e)

except Exception as e:
    print("An unexpected error occurred:")
    print(e)

finally:
    input("Press Enter to exit...")

# Test the ONNX model using onnxruntime
# Create an ONNX runtime session
ort_session = ort.InferenceSession(f"{onnx_output_directory}\\model.onnx")

# Define the input name and output name
input_name = ort_session.get_inputs()[0].name
output_name = ort_session.get_outputs()[0].name

# Run the ONNX model
y_pred_onnx = ort_session.run([output_name], {input_name: X_val})[0]

# Flatten the output array from ONNX
y_pred_onnx = y_pred_onnx.squeeze()

# Convert ONNX outputs to boolean (based on a threshold of 0.5)
y_pred_onnx_bool = y_pred_onnx > 0.5

# Initialize counters for ONNX model
correct_true_guess_onnx = 0
correct_false_guess_onnx = 0
incorrect_true_guess_onnx = 0
incorrect_false_guess_onnx = 0

# Iterate through true and predicted labels from ONNX model
for true_label, predicted_label in zip(y_val, y_pred_onnx_bool):
    if true_label == 1:
        if predicted_label == 1:
            correct_true_guess_onnx += 1
        else:
            incorrect_false_guess_onnx += 1
    else:
        if predicted_label == 0:
            correct_false_guess_onnx += 1
        else:
            incorrect_true_guess_onnx += 1

# Output counts for ONNX model
print(f"ONNX - Correct True Guess: {correct_true_guess_onnx}")
print(f"ONNX - Correct False Guess: {correct_false_guess_onnx}")
print(f"ONNX - Incorrect True Guess: {incorrect_true_guess_onnx}")
print(f"ONNX - Incorrect False Guess: {incorrect_false_guess_onnx}")

# Calculate accuracy for ONNX model
total_onnx = correct_true_guess_onnx + correct_false_guess_onnx + incorrect_true_guess_onnx + incorrect_false_guess_onnx
accuracy_onnx = (correct_true_guess_onnx + correct_false_guess_onnx) / total_onnx
print(f"ONNX Model Accuracy: {accuracy_onnx * 100:.2f}%")

if process.returncode == 0:
    print(f"Conversion successful. ONNX model is saved in {onnx_output_directory}.")
else:
    print("Conversion failed. Please check the command and environment.")

input("Press Enter to close the program...")
