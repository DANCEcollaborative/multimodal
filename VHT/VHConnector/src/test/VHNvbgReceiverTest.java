package test;

import javax.swing.*;

import edu.usc.ict.vhmsg.*;

import java.util.Arrays;
import java.util.LinkedList;
import java.util.Queue;
import java.util.concurrent.locks.*;

public class VHNvbgReceiverTest implements MessageListener {
	public static VHMsg vhmsg;

    public int numMessagesReceived = 0;

    private Lock lock = new ReentrantLock();
    private Lock vhmsgLock = new ReentrantLock();
    private Condition conConsumer = lock.newCondition();
    private Condition vhmsgConsumer = vhmsgLock.newCondition();

    private volatile Queue<String> messages = new LinkedList<String>();
    private volatile Queue<String> vhmsgs = new LinkedList<String>();
    
    public VHNvbgReceiverTest()
    {
        System.out.println("VHMSG_SERVER: " + System.getenv("VHMSG_SERVER"));
        System.out.println("VHMSG_SCOPE: " + System.getenv("VHMSG_SCOPE" ));

        vhmsg = new VHMsg();

        boolean ret = vhmsg.openConnection();
        if (!ret)
        {
            System.out.println("Connection error!");
            return;
        }

        vhmsg.enableImmediateMethod();
        vhmsg.addMessageListener(this);
        vhmsg.subscribeMessage("PSI_NVBG_Location");
        vhmsg.subscribeMessage("elbench");

        System.out.println( "VHReceiver Created" );
    }
    
    public void stop() {
        vhmsg.removeMessageListener(this);
    }

    public void subscribeMessage(String s) {
        vhmsg.subscribeMessage(s);
    }

    public void unsbuscribeMessage(String s) {
        vhmsg.unsubscribeMessage(s);
    }

	@Override
	public void messageAction(MessageEvent e) {
		// TODO Auto-generated method stub
        //System.out.println( "Received Message '" + e.toString() + "'" );
        numMessagesReceived++;
        System.out.println(numMessagesReceived + " messages received - '" + e.toString() + "'");
        addVhmsg(e.toString());
        String temp = e.toString();
        if (temp != null && temp.length() != 0) {
            StringBuilder sen = new StringBuilder();            
            sen.append(temp);            
            addMessage(sen.toString());
            System.out.println("addmessage: "+sen.toString());
        }
	}
	
	private void addVhmsg(String s) {
        vhmsgLock.lock();
        try{
            vhmsgs.offer(s);
            vhmsgConsumer.signalAll();
        }
        finally {
            vhmsgLock.unlock();
        }
    }

    private void addMessage(String s) {
        lock.lock();
        try{
            messages.offer(s);
            conConsumer.signalAll();
        }
        finally {
            lock.unlock();
        }
    }

    public String pollMessage() {
        String ret = "";
        lock.lock();
        try{
            while (messages.isEmpty()) {
                conConsumer.await();
            }
            ret = messages.poll();
        } catch (InterruptedException ignored) {

        } finally {
            lock.unlock();
        }
        return ret;
    }


    public String pollVhmsg() {
        String ret = "";
        vhmsgLock.lock();
        try{
            while (vhmsgs.isEmpty()) {
                vhmsgConsumer.await();
            }
            ret = vhmsgs.poll();
        } catch (InterruptedException ignored) {

        } finally {
            vhmsgLock.unlock();
        }
        return ret;
    }

}
