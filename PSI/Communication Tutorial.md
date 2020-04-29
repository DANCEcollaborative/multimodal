# Communication Tutorial

This is a simple tutorial file to introduce some basic concept of ActiveMQ and how to use the encapsulated version to communicate between C# and Java.

### Basis

To put it simply, ActiveMQ will open a port(default: 61616) for all the programs to communicate. There're two different types to send messages: Queue-based messages and Topic-based messages. In our project I only encapsulate the later type. 

The pipeline to communicate is:

* Both **subscribers** and **producers** should create a **CommunicationManager** instance to get registered to the same port.  

* **Subscriber** subscribes to a certain *topic* (represented by a string) for some types of messages(texts or byte arrays). In our case, the subscriber is the bazaar system.
* **Producer** post a message to some topic. Here Psi will be the producer.
* All the **subscriber **subscribed to this topic for same types of messages will received the message, and the ```onReceive``` callback function will be called by the **CommunicationManager**. 

### A quick demonstration
* Open the ActiveMQ on your computer(Follow the official Introduction: https://activemq.apache.org/getting-started (Installation Procedure & Starting ActiveMQ)).
* Open your java IDE, create a new project.
* Add `activemq-all-5.15.11.jar` and `communication.jar` to project external libraries.
* Run `smartlab.test.TestCommunication.main` in `communication.jar`. 
* Run  `CommunicationSample.exe` in `/java/C#Sender` (which is written in C#)

Now you can see that your main method in java received the message from the C# program!

### C# (Psi)

Currently, the C# end can only be the subscriber to send message to a certain topic. 

##### Preparation:

This package is contained in Psi package. To use the code below:

* Set Psi.Communication to the dependency list. 
* Download ActiveMQ Nuget package. 

##### Methods:

```C#
CommunicationManager manager = new CommunicationManager();
```

Create a communication manager registering to tcp://localhost:61616.

You can also specify your own address and port via following construction method, as long as both ends share the same one:

```C#
CommunicationManager(String uri);
CommunicationManager(int port);
```

After that, you can simply call ``SendText(String topic, String content)`` or `SendBytes(String topic, byte[] content)` to send message to a certain topic like:

```C#
manager.SendText("TextTopic", "This message will be sent to topic 'TextTopic'");
manager.SendBytes("BytesTopic", Encoding.Default.GetBytes("This message will be sent to topic 'BytesTopic'"));
```

### Java (Bazaar)

This java end will be a little more complex since it should process the message it received.

##### Preparation:

* Add the two .jar files in java directory to the project library. 

##### Methods:

Similarly, you can create your communication manager by:

```java
CommunicationManager manager = new CommunicationManager();
//CommunicationManager manager = new CommunicationManager(61616); (Specify port)
//CommunicationManager manager = new CommunicationManager("tcp://localhost:61616"); (Specify address)
```

Then you create a class which implement the corresponding `ISLSubscriber`  interface to receive certain type of message(In the example below, we'll only receive the text message. More example about bytes message and hybrid message is included in the TestCommunication sample). Implement the `onReceive` method to what you want to do after you receive the message:

```java
public class MySubscriber implements ISLTextSubscriber {
    @Override
    public void onReceive(String topic, String content) {
        System.out.println("Received string message. Topic: " + topic + "\tContent:" + content);
    }
}
```

Next, subscribe the your subscriber to any topic you want to listen to:

```java
MySubscriber subscriber = new MySubscriber();
manager.subscribe(subscriber, "TextTopic");
```

Now it's done! When a program sends text message to topic "TextTopic", `onReceive` method will be called. 

