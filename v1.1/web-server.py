from http.server import HTTPServer, CGIHTTPRequestHandler

port = 2000

server_address = ('', port)
httpd = HTTPServer(server_address, CGIHTTPRequestHandler)
print('Start')
httpd.serve_forever()
print('End')