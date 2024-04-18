###########################################
#
# goes throgh the folder and subfolders
#   if file name is "*.received.txt"
#     rename file to "*.verified.txt" (and overwrite existing file if any)
#
###########################################


import os
import subprocess

def accept_folder(directory: str):
    for (dirpath, dirnames, filenames) in os.walk(directory):
        for filename in filenames:
            if filename.endswith(".received.txt"):
                basename = filename[:-13]
                os.system(f"move /y {dirpath}\{basename}.received.txt {dirpath}\{basename}.verified.txt")
                print(f"accepted {basename}\n")


accept_folder(os.path.dirname(__file__));
accept_folder(os.path.join(os.path.dirname(__file__), "..", "BlazorTests"));
accept_folder(os.path.join(os.path.dirname(__file__), "..", "MinimalAPITests"));
