from mcstatus import JavaServer

def is_server_running():
    try:
        server = JavaServer.lookup("localhost")
        server.status()
        return True
    except Exception:
        return False

if is_server_running():
    print("The server is running.")
else:
    print("The server is not running.")
