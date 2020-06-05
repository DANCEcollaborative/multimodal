package smartlab.communication;
import org.apache.activemq.ActiveMQConnectionFactory;
import javax.jms.*;
import java.util.ArrayList;
import java.util.Map;

public class CommunicationManager {
    private ConnectionFactory factory;
    private Connection connection = null;
    private String uri;
    private Session session;

    private ArrayList<MessageConsumer> consumers;

    public CommunicationManager() {
        this(61616);
    }

    public CommunicationManager(int port) {
        this("tcp://localhost:" + port);
    }

    public CommunicationManager(String uri) {
        this.uri = uri;
        this.consumers = new ArrayList<>();
        initActiveMQServer();
    }

    private void initActiveMQServer() {
        factory = new ActiveMQConnectionFactory(this.uri);
        try {
            connection = factory.createConnection();
            //connection.setClientID("Customer");
            connection.start();
            session = connection.createSession(false, Session.AUTO_ACKNOWLEDGE);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    public void subscribe(ISLSubscriber subscriber, String topicString) {
        try {
            for (MessageConsumer consumer: consumers) {
                TopicListener listener = (TopicListener)consumer.getMessageListener();
                if (listener.getSubscriber().hashCode() == subscriber.hashCode()
                        && listener.getTopic().getTopicName().equals(topicString)) {
                    System.out.println(subscriber.toString() + "already subscribed topic " + topicString);
                    return;
                }
            }
            Topic destination = session.createTopic(topicString);
            MessageConsumer consumer = session.createConsumer(destination);
            consumers.add(consumer);
            consumer.setMessageListener(new TopicListener(subscriber, destination));
        } catch (JMSException e) {
            e.printStackTrace();
        }
    }

    public void unsubscribe(ISLSubscriber subscriber, String topicString) {
        for (MessageConsumer consumer: consumers) {
            try {
                TopicListener listener = (TopicListener)consumer.getMessageListener();
                if (listener.getSubscriber().hashCode() == subscriber.hashCode()
                    && listener.getTopic().getTopicName().equals(topicString)) {
                    consumers.remove(consumer);
                    break;
                }
            } catch (JMSException e) {
                e.printStackTrace();
            }
        }
    }
    
    public void msgSender (String topicString,String content) {
    	Topic destination;
		try {
			destination = session.createTopic(topicString);
	    	MessageProducer producer = session.createProducer(destination);
	    	TextMessage msg = session.createTextMessage(content);
	    	producer.send(destination, msg);
		} catch (JMSException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}

    }
}
