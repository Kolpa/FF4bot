from dropbox import DropboxConnection
import sys

args = sys.argv

conn = DropboxConnection(args[1],args[2])
conn.upload_file("FF4Bot/bin/Debug/FF4Bot.exe","/Public","FF4Bot.exe")