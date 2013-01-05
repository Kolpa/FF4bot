from dropbox import DropboxConnection
import sys
import zipfile as zip

args = sys.argv

file = zip.ZipFile("Bot.zip","w")
file.write("FF4Bot/bin/Debug/FF4Bot.exe")
file.write("FF4Bot/bin/Debug/InputSimulator.dll")
file.close()

conn = DropboxConnection(args[1],args[2])
conn.upload_file("Bot.zip","/Public","Bot.zip")