from http.server import HTTPServer, CGIHTTPRequestHandler

server_address = ('', 2000)
httpd = HTTPServer(server_address, CGIHTTPRequestHandler)
print('Start')
httpd.serve_forever()
print('End')