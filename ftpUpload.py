import ftplib
import sys

args = sys.argv

ftp = ftplib.FTP(args[1])
print("connected")

ftp.login(args[2],args[3])
print("login done")
directory = args[5]
ftp.cwd(directory)

filename = args[4]

file = open(filename, 'rb')
ftp.storbinary('STOR '+args[6], file)
print("saved")
file.close()
ftp.quit()
print("disconnected")