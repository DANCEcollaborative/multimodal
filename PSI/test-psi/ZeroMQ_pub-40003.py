import msgpack
import zmq, random, datetime, json, time

context = zmq.Context()
socket = context.socket(zmq.PUB)
socket.bind('tcp://*:40003')
text_choices = ["location:left", "location:front", "location:right", "speech:I'm Rachel"]

dict_left = {
        "location":"left", "pose":"handraise",
}
dict_front = {
    "location":"front", "pose":"handraise",
}
dict_right = {
    "location":"right", "speech":"I'm Rory",
}
dict_speech = {
        "speech":"I'm Rory", "pose":"handraise",
}

dict_choices = [dict_left, dict_front, dict_right, dict_left, dict_front, dict_right, dict_speech]

def generate_current_dotnet_datetime_ticks(base_time = datetime.datetime(1, 1, 1)):
    return (datetime.datetime.utcnow() - base_time)/datetime.timedelta(microseconds=1) * 1e1

while True:
    payload = {}
    dict_choice = random.choice(dict_choices)
    numKeys = len(dict_choice)
    i=1
    for c in dict_choice:
        if i < numKeys:
            print(c,':',dict_choice[c], end="  --  ")
        else:
            print(c, ':', dict_choice[c])
        i+=1
    payload['message'] = dict_choice
    payload['originatingTime'] = generate_current_dotnet_datetime_ticks()
    socket.send_multipart(['Remote_PSI_Text'.encode(), msgpack.dumps(payload)])
    time.sleep(10)
