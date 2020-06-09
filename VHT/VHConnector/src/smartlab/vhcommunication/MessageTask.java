package smartlab.vhcommunication;
import java.util.*;
import java.util.concurrent.ConcurrentHashMap;

import javax.jms.JMSException;

import smartlab.vhmsgprocessor.*;

public class MessageTask implements Runnable{
	String content;
	String type;
	private final Map<String,String> identityInfo = new ConcurrentHashMap<>();
	VHSender sender = VHSender.getInstance();
	
	public MessageTask(String content, String type) {
		this.content = content;
		this.type = type;
	}
	@Override
	public void run() {
		sender.sendMessage(this.content, this.type);
		System.out.println("Present Thread:"+Thread.currentThread().getName()+"Content is :"+this.content+"Type is :" +this.type);		
	}

}
