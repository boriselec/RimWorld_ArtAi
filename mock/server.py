from http.server import BaseHTTPRequestHandler, HTTPServer
from os import curdir, sep
from time import time
import sys

hostName = "localhost"
serverPort = 8080

class Mock(BaseHTTPRequestHandler):
    result = {}

    def do_POST(self):
        if self.path.startswith('/generate'):
            self.send_response(200)
            content_length = int(self.headers['Content-Length'])
            post_data = self.rfile.read(content_length).decode('utf-8')
            sys.stderr.write("%s\n" % post_data)
            generate_time = self.result.get(post_data)
            if generate_time is None:
                self.send_header("Content-type", "text")
                self.end_headers()
                self.result[post_data] = time()
                result = 'Loading...'
                self.wfile.write(bytes('Queued...', "utf-8"))
            elif time() < generate_time + 20:
                self.send_header("Content-type", "text")
                self.end_headers()
                self.wfile.write(bytes(post_data, "utf-8"))
            else:
                f = open(curdir + sep + 'mock.png', 'rb') 
                self.send_header("Content-type", "image/png")
                self.end_headers()
                self.wfile.write(f.read())
                f.close()
        else:
            self.send_error(404, 'Not found')

if __name__ == "__main__":
    webServer = HTTPServer((hostName, serverPort), Mock)
    print("Server started http://%s:%s" % (hostName, serverPort))

    try:
        webServer.serve_forever()
    except KeyboardInterrupt:
        pass

    webServer.server_close()
    print("Server stopped.")
