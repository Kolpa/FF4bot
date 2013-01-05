from dropbox import DropboxConnection
import sys
import zipfile as zip
import os
args = sys.argv

print("[1/4] Moving Files...")
os.rename("FF4Bot/bin/Debug/FF4Bot.exe", "FF4Bot.exe")
os.rename("FF4Bot/bin/Debug/InputSimulator.dll", "InputSimulator.dll")
print("[1/4] done")

print("[2/4] Creating Zip...")
file = zip.ZipFile("Bot.zip", "w")
file.write("FF4Bot.exe")
file.write("InputSimulator.dll")
file.close()
print("[2/4] done")

print("[3/4] Connecting..")
conn = DropboxConnection(args[1], args[2])
print("[3/4] done")

print("[4/4] Uploading...")
conn.upload_file("Bot.zip", "/Public", "Bot.zip")
print("[4/4] done")