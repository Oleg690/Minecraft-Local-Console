import os, json, sys

# Set the content type to JSON for the response
print("Content-Type: application/json")
print()

try:
    # Check if the request method is POST
    if os.environ['REQUEST_METHOD'] == 'POST':
        try:
            # Get the content length and read the request data
            content_length = int(os.environ.get('CONTENT_LENGTH'))
            request_data = sys.stdin.read(content_length)
            
            # Parse the JSON request data
            data = json.loads(request_data)
        except (ValueError, KeyError) as e:
            # Handle errors in JSON parsing or environment variables
            print(json.dumps(['Error', 'Invalid request data or missing environment variable.']))
            sys.exit(1)

        # Retrieve data fields from JSON
        lastPath = data.get("lastPath")
        worldNumber = data.get("worldNumber")
        values = data.get("values")

        # Attempt to write data to the specified file
        try:
            with open(lastPath, 'w', encoding='utf-8') as file:
                file.write(f"{values}")
        except FileNotFoundError:
            print(json.dumps(['Error', f"File '{lastPath}' not found."]))
            sys.exit(1)
        except IOError:
            print(json.dumps(['Error', "Permission error when writing to the file."]))
            sys.exit(1)
        except Exception as e:
            print(json.dumps(['Error', f"An unexpected error occurred: {str(e)}"]))
            sys.exit(1)

        # Confirm successful write operation
        print(json.dumps(['Success', "Data updated successfully!"]))
    else:
        # Handle unsupported request methods
        print(json.dumps(['Error', 'Only POST requests are supported for this endpoint!']))

except Exception as e:
    # General catch-all for unexpected errors
    print(json.dumps(['Error', f"An unexpected error occurred: {str(e)}"]))
