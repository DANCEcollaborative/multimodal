import stomp


class CommunicationManager():
    """
        This class is used to communicate with other part(PSI etc.) through stomp(ActiveMQ).
        Generally, you only need to get one instance to handle all the communication works.
    """
    def __init__(self):
        """
        Initialize a manager.
        """
        self.topics = {}
        self.connections = {}
        self.num = 0
        self.conn = stomp.Connection()
        self.conn.connect()

    def send(self, topic, msg):
        """
            Send a message through stomp protocol.
            NOTICE: with stomp, python can only send byte messages.
        :param topic:
            The topic in ActiveMQ.
            Note that prefix /topic/ is used in original stomp protocol, but in ActiveMQ it is automatically omitted.
            Thus we add the /topic/ prefix before to get compatible with ActiveMQ.
        :param msg:
            The message to send. It will be sent as a byte message whatever its original type is.
        """
        self.conn.send(body=msg, destination="/topic/%s" % topic)

    def subscribe(self, listener, topic):
        """
            Subscribe a listener to a given topic.
            Different listeners are allowed to subscribe to the same topic, and a listener is allowed to subscribe to
            different topics.
            A listener must implement `on_message` method to handle the incoming message. Headers (containing the topic
            information in the `destination` field) and contents of the stomp message will be passed separately.
        :param listener:
            The subscriber. on_message(self, headers, massage) should be implemented to handle the incoming message.
        :param topic:
            The subscribed topic.
        """
        if id(listener) in self.connections:
            # Prevent subscribe to a topic for multiple times.
            if topic in self.topics[id(listener)]:
                print("Listener %s has already subscribed to topic %s." % (str(listener), topic))
                return
            else:
                self.connections[id(listener)].subscribe('/topic/%s' % topic)
                self.topics[id(listener)].append(topic)
        else:
            # Create a new stomp connection for a new listener.
            conn = stomp.Connection10()
            conn.set_listener("listener_%d_%d" % (self.num, id(listener)), listener)
            conn.connect()
            conn.subscribe('/topic/%s' % topic)
            self.num += 1
            self.connections[id(listener)] = conn
            self.topics[id(listener)] = [topic]

    def unsubscribe(self, listener, topic):
        """
            Unsubscribe a listener to a given topic.
        :param listener:
            The subscriber.
        :param topic:
            The topic to unsubscribe.
        """
        if id(listener) in self.connections:
            if topic not in self.topics[id(listener)]:
                print("Listener %s hasn't subscribed to topic %s" % (str(listener), topic))
                return
            else:
                self.connections[id(listener)].unsubscribe("/topic/%s" % topic)
                self.topics[id(listener)].remove("topic")
        else:
            print("Listener %s hasn't subscribed to anything" % (str(listener)))

    def __del__(self):
        for listener_id in self.connections:
            self.connections[listener_id].disconnect()
        self.conn.disconnect()
