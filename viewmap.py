import csv
from PIL import Image, ImageTk
from tkinter import Tk, Canvas

import csv
from PIL import Image, ImageTk
from tkinter import Tk, Canvas

def animate_map_from_csv(filename, scale=2):
    # Open the CSV file and read the values into a 2D list
    with open(filename, 'r') as file:
        reader = csv.reader(file)
        values = [[int(value) for value in row] for row in reader]

    # Create a new window with a canvas
    root = Tk()
    canvas = Canvas(root, width=len(values[0])*scale, height=len(values)*scale)
    canvas.pack()

    # Draw the initial image on the canvas
    image = Image.new('RGB', (len(values[0])*scale, len(values)*scale))
    for x in range(len(values)):
        for y in range(len(values[0])):
            value = values[x][y]
            color = (value, value, value)
            for i in range(scale):
                for j in range(scale):
                    image.putpixel((y*scale+j, x*scale+i), color)
    image_tk = ImageTk.PhotoImage(image)
    canvas.create_image(0, 0, anchor='nw', image=image_tk)

    # Define a function to update the image periodically
    def update_image():
        # Read the new values from the CSV file
        with open(filename, 'r') as file:
            reader = csv.reader(file)
            values = [[int(value) for value in row] for row in reader]

        # Update the image with the new values
        for x in range(len(values)):
            for y in range(len(values[0])):
                value = values[x][y]
                color = (value, value, value)
                for i in range(scale):
                    for j in range(scale):
                        image.putpixel((y*scale+j, x*scale+i), color)
        image_tk.paste(image)

        # Schedule the next update
        root.after(1000, update_image)

    # Schedule the first update
    root.after(1000, update_image)

    # Start the main event loop of the GUI
    root.mainloop()



animate_map_from_csv('./maps/map_ef2384aa9b6fd395decd3888e5cbd05ea6cccf64.csv')