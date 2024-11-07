import os, json, sys

print("Content-Type: application/json")
print()

if os.environ['REQUEST_METHOD'] == 'POST':
    content_length = int(os.environ['CONTENT_LENGTH'])
    request_data = sys.stdin.read(content_length)
    
    data = json.loads(request_data)
    
    lastPath = data.get("lastPath")
    worldNumber = data.get("worldNumber")
    values = data.get("values")
    

    file = open(lastPath, 'w', encoding='utf-8')
    file.write(f"{values}")
    file.close()

    try:
        with open('lastPath', 'w', encoding='utf-8') as file:
            file.write(values)
            file.close()
            print(json.dumps(['Success', "Data updated successfully!"]))
    except IOError as e:
        print(json.dumps(['Error', "Permision error!"]))

else:
    result = ['Error', 'Only POST requests are supported for this endpoint!']
    print(json.dumps(result))
