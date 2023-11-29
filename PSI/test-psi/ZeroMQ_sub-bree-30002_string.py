import zmq, json, time, msgpack

socket = zmq.Context().socket(zmq.SUB)
socket.connect("tcp://128.2.204.249:30002")
socket.setsockopt(zmq.SUBSCRIBE, b'') # '' means all topics

while True:
    [topic, payload] = socket.recv_multipart()
    # message = msgpack.unpackb(payload, raw=True)
    message = msgpack.unpackb(payload, raw=True)
    frame = message[b"message"]
    # for keys,values in message.items()
    #    print(keys)
    #    print(values)
    # j = json.loads(message)
    print(frame)
    # for key in frame:
    #    print(key)
    #    for value in message[key]:
    #        print(value)
    # print ("ZeroMQ_sub received: ", repr(j['message']))
