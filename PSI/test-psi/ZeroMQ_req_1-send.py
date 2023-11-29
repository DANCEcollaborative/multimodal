import zmq, datetime, time, json, msgpack

context = zmq.Context()
socket = context.socket(zmq.REQ)

print("Connecting to server...")
socket.connect("tcp://128.2.204.249:40001")   # bree
time.sleep(1)
request = "tcp://128.2.220.118:40003"     # erebor

# Send the request
payload = {}
payload['message'] = request
payload['originatingTime'] = datetime.datetime.utcnow().isoformat()
print(f"Sending request: {request}")
socket.send_string(request)

#  Get the reply
reply = socket.recv()
print(f"Received reply: {reply}")
