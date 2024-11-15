from serverUtilities import runCommand
import cgi, json

print('Content-type: text/html\n')

params = cgi.FieldStorage()
action = params.getfirst('action')
worldNumber = params.getfirst('number', '')

runCommand(action, worldNumber)