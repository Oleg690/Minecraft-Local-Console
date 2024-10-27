import json

file = open('logs\\latest.log', 'r', encoding='utf-8')
data = file.read()
file.close()

print("Content-type: text/html\n")

print(json.dumps(data))