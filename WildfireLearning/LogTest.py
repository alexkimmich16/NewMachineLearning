import onnxruntime as ort
import numpy as np

# Initialize counters for the four categories
correct_true_guess = 0
correct_false_guess = 0
incorrect_true_guess = 0
incorrect_false_guess = 0

# Load the ONNX model
ort_session = ort.InferenceSession("B:/GitProjects/NewMachineLearning/NewMachineLearning/Assets/Scripts/PyTest/model.onnx")

# Get the input and output names
input_name = ort_session.get_inputs()[0].name
output_name = ort_session.get_outputs()[0].name

# Run ONNX model on the same validation data
y_pred_onnx = ort_session.run([output_name], {input_name: X_val})[0]

# Convert the predicted values to boolean labels based on a 0.5 threshold
y_pred_onnx_bool = y_pred_onnx.squeeze() > 0.5

# Iterate through the true and predicted labels to update the counters
for true_label, predicted_label in zip(y_val, y_pred_onnx_bool):
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

# Output the counts
print(f"Correct True Guess (ONNX): {correct_true_guess}")
print(f"Correct False Guess (ONNX): {correct_false_guess}")
print(f"Incorrect True Guess (ONNX): {incorrect_true_guess}")
print(f"Incorrect False Guess (ONNX): {incorrect_false_guess}")

# Calculate the accuracy based on the counts
total = correct_true_guess + correct_false_guess + incorrect_true_guess + incorrect_false_guess
accuracy_onnx = (correct_true_guess + correct_false_guess) / total
print(f"Model Accuracy (ONNX): {accuracy_onnx * 100:.2f}%")
