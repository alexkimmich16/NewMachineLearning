import json
import numpy as np
import tensorflow as tf
from sklearn.model_selection import train_test_split
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import LSTM, Dense, Dropout, BatchNormalization
from tensorflow.keras.regularizers import l2
from tensorflow.keras.callbacks import EarlyStopping
from sklearn.utils.class_weight import compute_class_weight

# Read data from JSON file
with open('Fireball.json', 'r') as file:
    data = json.load(file)

# Define the number of frames
num_frames = 6

# Extract sequences of num_frames consecutive frames
sequences = []
labels = []
for motion in data['Motions']:
    for i in range(len(motion['Frames']) - num_frames + 1):
        sequence = [
            [
                info['info']['HandPos']['x'], info['info']['HandPos']['y'], info['info']['HandPos']['z'],
                info['info']['HandRot']['x'], info['info']['HandRot']['y'], info['info']['HandRot']['z'],
                info['TimeSinceLast']
            ] for info in motion['Frames'][i:i+num_frames]
        ]
        sequences.append(sequence)
        label = any(info['Active'] for info in motion['Frames'][i:i+num_frames])
        labels.append(int(label))

# Convert to numpy arrays
X = np.array(sequences, dtype='float32')
y = np.array(labels, dtype='float32')

# Compute class weights
class_weights = compute_class_weight('balanced', classes=np.unique(y), y=y)
class_weight_dict = {i: weight for i, weight in enumerate(class_weights)}

# Split data into training and validation sets
X_train, X_val, y_train, y_val = train_test_split(X, y, test_size=0.2)

# Define batch size
batch_size = 128

# Convert to TensorFlow datasets
train_dataset = tf.data.Dataset.from_tensor_slices((X_train, y_train)).cache().batch(batch_size).prefetch(buffer_size=tf.data.AUTOTUNE)
val_dataset = tf.data.Dataset.from_tensor_slices((X_val, y_val)).cache().batch(batch_size).prefetch(buffer_size=tf.data.AUTOTUNE)

# Define LSTM model
model = Sequential([
    LSTM(512, return_sequences=True, kernel_regularizer=l2(0.001), input_shape=(num_frames, X_train.shape[2])),
    BatchNormalization(),
    Dropout(0.5),
    LSTM(512, return_sequences=True, kernel_regularizer=l2(0.001)),
    BatchNormalization(),
    Dropout(0.5),
    LSTM(256, return_sequences=True, kernel_regularizer=l2(0.001)),
    BatchNormalization(),
    Dropout(0.5),
    LSTM(256, return_sequences=False, kernel_regularizer=l2(0.001)),
    BatchNormalization(),
    Dropout(0.5),
    Dense(256, activation='relu', kernel_regularizer=l2(0.001)),
    Dropout(0.5),
    Dense(128, activation='relu', kernel_regularizer=l2(0.001)),
    Dropout(0.5),
    Dense(1, activation='sigmoid')
])

# Compile the model with adjusted learning rate
model.compile(optimizer=tf.keras.optimizers.Adam(learning_rate=0.001), loss='binary_crossentropy', metrics=['accuracy'])

# Early stopping callback
early_stopping = EarlyStopping(monitor='val_loss', patience=5, restore_best_weights=True)

# Train the model with class weights
model.fit(train_dataset, epochs=50, validation_data=val_dataset, callbacks=[early_stopping], class_weight=class_weight_dict)

# Evaluate the model
loss, accuracy = model.evaluate(val_dataset)
print(f"Model Accuracy: {accuracy * 100:.2f}%")

# Save the model
model_path = 'saved_model'
model.save(model_path)

# Specify the directory where the ONNX file will be saved
onnx_output_directory = r"B:\GitProjects\NewMachineLearning\NewMachineLearning\Assets\Scripts\PyTest"

print("SavedModel is ready. Converting to ONNX...")
onnx_command = f"python -m tf2onnx.convert --saved-model saved_model --output {onnx_output_directory}/model.onnx"
process = subprocess.run(onnx_command, shell=True, check=True)

if process.returncode == 0:
    print(f"Conversion successful. ONNX model is saved in {onnx_output_directory}.")
else:
    print("Conversion failed. Please check the command and environment.")

input("Press Enter to close the program...")
