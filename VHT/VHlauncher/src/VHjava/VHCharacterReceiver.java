package VHjava;
import edu.usc.ict.vhmsg.*;

import javax.swing.*;
import java.util.Arrays;
import java.util.LinkedList;
import java.util.Queue;
import java.util.concurrent.locks.*;

public class VHCharacterReceiver implements MessageListener {

    public static VHMsg vhmsg;

    public int numMessagesReceived = 0;

    private Lock lock = new ReentrantLock();
    private Lock vhmsgLock = new ReentrantLock();
    private Condition conConsumer = lock.newCondition();
    private Condition vhmsgConsumer = vhmsgLock.newCondition();

    private volatile Queue<String> messages = new LinkedList<String>();
    private volatile Queue<String> vhmsgs = new LinkedList<String>();

    public VHCharacterReceiver()
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
        vhmsg.subscribeMessage("VHBuilder");
        vhmsg.subscribeMessage("acquireSpeech");
        vhmsg.subscribeMessage("vhtspeechrecorder");
        vhmsg.subscribeMessage("vrSpeech");
        vhmsg.subscribeMessage("sbm");
        vhmsg.subscribeMessage("vrSpoke");
        vhmsg.subscribeMessage("test");
        vhmsg.subscribeMessage("RemoteSpeechCmd");
        vhmsg.subscribeMessage("TTSRelay");
        vhmsg.subscribeMessage("TTSRelayCacheClear");
        vhmsg.subscribeMessage("playsound");
        vhmsg.subscribeMessage("vrAgentBML");
        vhmsg.subscribeMessage("vrProcEnd");
        vhmsg.subscribeMessage("launcher");

        System.out.println( "VHReceiver Created" );
    }

    @Override
    public void messageAction( MessageEvent e ) {
        //System.out.println( "Received Message '" + e.toString() + "'" );
        numMessagesReceived++;
        System.out.println(numMessagesReceived + " messages received - '" + e.toString() + "'");
        addVhmsg(e.toString());
        String[] temp = e.toString().split(" ");
        if (temp[0].equals("vrSpeech") && temp[1].equals("interp")) {
            StringBuilder sen = new StringBuilder();
            for (int i = 6; i < temp.length; ++i) {
                sen.append(temp[i]).append(" ");
            }
            addMessage(sen.toString());
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

    public void stop() {
        vhmsg.removeMessageListener(this);
    }
}
