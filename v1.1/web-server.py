import logging
from http.server import HTTPServer, CGIHTTPRequestHandler

# Configure logging
logging.basicConfig(level=logging.DEBUG)  # Set log level to DEBUG for detailed output

port = 4000

server_address = ('', port)
httpd = HTTPServer(server_address, CGIHTTPRequestHandler)

logging.info(f'Starting server on port {port}')
try:
    httpd.serve_forever()
except KeyboardInterrupt:
    logging.info("Server interrupted")
except Exception as e:
    logging.error(f"Error occurred: {e}")
finally:
    logging.info("Shutting down server")
