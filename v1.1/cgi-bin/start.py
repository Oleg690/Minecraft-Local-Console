import subprocess

bat_file = r"start.bat"

subprocess.Popen(['start', 'cmd', '/k', bat_file], shell=True)