import cv2
import numpy as np
import os
import glob

def remove_green_screen(img_path, out_path):
    # Read the image
    img = cv2.imread(img_path)
    if img is None:
        print(f"Failed to load {img_path}")
        return
        
    # Convert to HSV color space
    hsv = cv2.cvtColor(img, cv2.COLOR_BGR2HSV)
    
    # Define green screen color range (typical bright green screen)
    lower_green = np.array([35, 40, 40])
    upper_green = np.array([85, 255, 255])
    
    # Create mask of the green background (255 for green background, 0 for ship)
    mask = cv2.inRange(hsv, lower_green, upper_green)
    
    # Invert mask so the ship is white (255) and background is black (0)
    alpha = cv2.bitwise_not(mask)
    
    # To create a soft Alpha gradient near the ship:
    # 1. Apply a small morphological closing to fill tiny holes in the ship
    kernel_close = np.ones((3,3), np.uint8)
    alpha = cv2.morphologyEx(alpha, cv2.MORPH_CLOSE, kernel_close)
    
    # 2. Erode the mask slightly so the boundary moves inwards (removing the green edge)
    kernel_erode = np.ones((3,3), np.uint8)
    alpha_eroded = cv2.erode(alpha, kernel_erode, iterations=1)
    
    # 3. Blur the eroded mask to create a soft, smooth alpha gradient
    alpha_soft = cv2.GaussianBlur(alpha_eroded, (5,5), 0)
    
    # Suppress green spill on the edges
    b, g, r = cv2.split(img)
    # Limit the green channel to the maximum of red and blue to remove the green halo
    g_suppressed = np.minimum(g, np.maximum(r, b))
    
    # Create an edge mask (where alpha is between 0 and 255)
    # We use a broader range for the edge to ensure green spill is removed in the soft transition
    edge_mask = (alpha_soft > 0) & (alpha_soft < 255)
    
    # Apply green suppression only on the edges
    g_final = np.where(edge_mask, g_suppressed, g)
    
    # Combine to form BGRA image
    rgba = cv2.merge([b, g_final.astype(np.uint8), r, alpha_soft])
    
    # Save the output
    cv2.imwrite(out_path, rgba)
    print(f"Processed: {out_path}")

def main():
    input_dir = "generated-images"
    output_dir = "generated-images-transparent"
    
    if not os.path.exists(output_dir):
        os.makedirs(output_dir)
        
    images = glob.glob(os.path.join(input_dir, "*.png"))
    images.extend(glob.glob(os.path.join(input_dir, "*.jpg")))
    
    if len(images) == 0:
        print("No images found in the 'generated-images' directory.")
        return
        
    print(f"Found {len(images)} images. Processing...")
    for img_path in images:
        name = os.path.basename(img_path)
        out_path = os.path.join(output_dir, name.replace('.jpg', '.png'))
        remove_green_screen(img_path, out_path)
        
    print("All images processed successfully! Check the 'generated-images-transparent' directory.")

if __name__ == "__main__":
    main()
