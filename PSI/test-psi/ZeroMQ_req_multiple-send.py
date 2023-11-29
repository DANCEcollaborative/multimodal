import zmq, datetime, time, json, msgpack

context = zmq.Context()
socket = context.socket(zmq.REQ)

print("Connecting to server...")
socket.connect("tcp://128.2.204.249:40001")   # bree
time.sleep(1)
# request = "tcp://128.2.220.118:40003"     # erebor
request = json.dumps({"sensorVideoText":"tcp://128.2.220.118:40003", "sensorAudio": "tcp://128.2.212.138:40001", "sensorDOA": "tcp://128.2.212.138:40002", "sensorVAD": "tcp://128.2.212.138:40003"})   # erebor"
    # request = "tcp://128.2.149.108:40003"
    # request = "tcp://23.227.148.141:40003"
    # "sensorVideoText":"tcp://128.2.212.138:40000" -- Nano
    # "sensorVideoText":"tcp://128.2.220.118:40003" -- erebor

# Send the request
payload = {}
payload['message'] = request
payload['originatingTime'] = datetime.datetime.utcnow().isoformat()
print(f"Sending request: {request}")
socket.send_string(request)

#  Get the reply
reply = socket.recv()
print(f"Received reply: {reply}")
