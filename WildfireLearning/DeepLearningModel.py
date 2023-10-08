import json
import os
import subprocess
import openpyxl
import numpy as np
import onnxruntime as ort
import tensorflow as tf
import pandas as pd
from sklearn.model_selection import train_test_split
from sklearn.metrics import confusion_matrix, accuracy_score
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import Conv1D, MaxPooling1D, Flatten, Dense, Dropout, BatchNormalization
from tensorflow.keras.regularizers import l2
from tensorflow.keras.callbacks import EarlyStopping
from sklearn.utils.class_weight import compute_class_weight
from itertools import chain

# Get a list of all .json files in the current directory
json_files = [f for f in os.listdir() if os.path.isfile(f) and f.endswith('.json')]

# Display options to the user
print("Select a motion to bake:")
for index, filename in enumerate(json_files):
    print(f"{index}. {filename}")
print(f"{len(json_files)}. Bake All")

while True:
    try:
        choice = int(input("Enter your choice: "))
        if 0 <= choice <= len(json_files):  # ensures choice is within the range
            break
        else:
            print("Please enter a valid choice from the given options.")
    except ValueError:
        print("Invalid input. Please enter a number corresponding to your choice.")

if choice == len(json_files):
    files_to_process = json_files
else:
    files_to_process = [json_files[choice]]

for file_name in files_to_process:
    
    with open(file_name, 'r') as file:
        data = json.load(file)

    # Define the number of frames
    num_frames = 2
    StateCount = 2

    # Extract sequences
    sequences = []
    labels = []
    DoneDebugging = False  # flag to indicate if the first motion has been processed

    # Create an empty list to hold the Excel data
    for motion in data['Motions']:
        # Start from num_frames and go to the last frame
        for i in range(num_frames, len(motion['Frames']) + 1):
            sequence = [[float(value) for value in info['FrameInfo']] for info in motion['Frames'][i-num_frames:i]]

            # Build debug string and Excel row
            debug_str = f"frame: {i} "
            excel_row = []
            for frame_index, frame in enumerate(sequence):
                for input_index, input_value in enumerate(frame):
                    rounded_value = round(input_value, 5)
                    debug_str += f"'Inputs[{input_index + frame_index * len(frame)}]':{rounded_value} "
                    excel_row.append(f"{rounded_value}")

            # If this is the first frame of the first motion, write to Excel
            sequences.append(sequence)
            label = int(motion['Frames'][i - 1]['State'])  # Already updated in the provided code
            labels.append(label)


    # Convert to numpy arrays
    X = np.array(sequences, dtype='float32')
    y = np.array(labels, dtype='float32')

    # Compute class weights
    class_weights = compute_class_weight('balanced', classes=np.unique(y), y=y)
    class_weight_dict = {i: weight for i, weight in enumerate(class_weights)}

    # Split data into training and validation sets
    X_train, X_val, y_train, y_val = train_test_split(X, y, test_size=0.15, shuffle=True, random_state=42)

    model = Sequential([
        Conv1D(32, 2, activation='relu', input_shape=(num_frames, X_train.shape[2])),
        BatchNormalization(),
        Dropout(0.5),
        Flatten(),
        Dense(48, activation='relu'),
        Dropout(0.5),
        Dense(StateCount, activation='softmax')  # Note the change to 3 neurons and softmax activation
    ])

    # Compile the model
    model.compile(optimizer='adam', loss='sparse_categorical_crossentropy', metrics=['accuracy'])

    # Early stopping callback
    early_stopping = EarlyStopping(monitor='val_loss', patience=5, restore_best_weights=True)

    # Train the model with class weights
    model.fit(X_train, y_train, epochs=50, batch_size=64, validation_data=(X_val, y_val), callbacks=[early_stopping], class_weight=class_weight_dict, verbose=0)
    # Initialize counters

    # Evaluate the model on the validation set
    y_pred = model.predict(X_val)
    y_pred_classes = np.argmax(y_pred, axis=1)  # Convert softmax outputs to class indices

    # Save the model
    model_identifier = os.path.splitext(file_name)[0]
    RawSpellName = model_identifier.replace(".json", "")
    model_path = f'saved_model_{model_identifier}'
    model.save(model_path)



    try:
        # Convert to ONNX
        onnx_output_directory = f"B:\\GitProjects\\NewMachineLearning\\NewMachineLearning\\Assets\\Scripts\\AthenaExport"
        onnx_command = f"python -m tf2onnx.convert --saved-model {model_path} --output {onnx_output_directory}\\{RawSpellName}.onnx"

        process = subprocess.run(onnx_command, shell=True, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

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

    # Test the ONNX model using onnxruntime
    # Create an ONNX runtime session
    ort_session = ort.InferenceSession(f"{onnx_output_directory}\\model.onnx")

    # Define the input name and output name
    input_name = ort_session.get_inputs()[0].name
    output_name = ort_session.get_outputs()[0].name

    # Run the ONNX model
    y_pred_onnx = ort_session.run([output_name], {input_name: X_val})[0]

    # Flatten the output array from ONNX
    y_pred_onnx_classes = np.argmax(y_pred_onnx, axis=1)

    # Initialize counters for ONNX model
    confusion_counts = {}
    for actual_state in range(StateCount):
        for predicted_state in range(StateCount):
            confusion_counts[(actual_state, predicted_state)] = 0

    # Iterate through true and predicted labels from ONNX model
    for true_label, predicted_label in zip(y_val, y_pred_onnx_classes):
        confusion_counts[(int(true_label), int(predicted_label))] += 1

    # To print the counts:
    for actual_state in range(StateCount):
        for predicted_state in range(StateCount):
            print(f"True State {actual_state}, Predicted State {predicted_state}: {confusion_counts[(actual_state, predicted_state)]}")

    # Calculate accuracy for ONNX model
    conf_mat_onnx = confusion_matrix(y_val, y_pred_onnx_classes)
    accuracy_onnx = accuracy_score(y_val, y_pred_onnx_classes)

    print(f"ONNX Confusion Matrix:\n{conf_mat_onnx}")
    print(f"ONNX Model Accuracy: {accuracy_onnx * 100:.2f}%")

    if process.returncode == 0:
        print(f"Conversion successful. ONNX model is saved in {onnx_output_directory}.")
    else:
        print("Conversion failed. Please check the command and environment.")

input("Press Enter to close the program...")