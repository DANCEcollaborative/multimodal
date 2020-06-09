package smartlab.vhcommunication;
import java.io.*;
import edu.usc.ict.vhmsg.*;
import smartlab.vhmsgprocessor.*;

/*
 * Send the VHmsg as needed,both text and NVB message.
 */
public class VHSender {

    public static VHMsg vhmsg;
    public String name = "Rachel";
    public String msgtype = "speech";

    public int numMessagesReceived = 0;
    public int m_testSpecialCases = 0;
    VHMsgSpliter vhmsgspliter = VHMsgSpliter.getInstance();
    NVBMsgProcessor nvbMsg = new NVBMsgProcessor();
    TextMsgProcessor textMsg = new TextMsgProcessor();
    
	private static VHSender instance = new VHSender();
	public static VHSender getInstance() {
		return instance;
	}

    /*
     * get the name of current Char in the VHT
     */
    public void setChar(String name) {
        this.name = name;
    }

    private boolean kbhit()
    {
        try
        {
            return ( System.in.available() != 0 );
        }
        catch (IOException ignored)
        {
        }
        return false;
    }

    public VHSender()
    {
        System.out.println( "VHMSG_SERVER: " + System.getenv( "VHMSG_SERVER" ) );
        System.out.println( "VHMSG_SCOPE: " + System.getenv( "VHMSG_SCOPE" ) );

        vhmsg = new VHMsg();

        boolean ret = vhmsg.openConnection();
        if ( !ret )
        {
            System.out.println( "Connection error!" );
            return;
        }
        System.out.println( "VHSender Created" );
    }

    /*
     * Send the text or Non-verbal behavior(NVB) message basing 
     * @param name : String
     * name of the Char
     * @param content : String
     * the content received from PSI
     * @param msgtype : String
     * the message type of the received message(text/nvb)
     */
    public void sendMessage(String name, String content, String msgtype) {
        if (msgtype.equals("speech")) {
        	System.out.println("!!!!Messages to!!!!"+name+content+msgtype);
        	vhmsg.sendMessage(textMsg.constructTextMsg(name, content));
        }
        else if (msgtype.equals("location")) {
        	vhmsg.sendMessage(nvbMsg.constructNVBMsg(name, content));
        }    	
    }
    
    /*
     * Send the text or Non-verbal behavior(NVB) message basing 
     * @param content : String
     * the content received from PSI
     * @param msgtype : String
     * the message type of the received message(text/nvb)
     */
    public void sendMessage(String content, String msgtype) {
        if (msgtype.equals("speech")) {
        	vhmsg.sendMessage(textMsg.constructTextMsg(this.name, content));
        }
        else if (msgtype.equals("location")) {
        	vhmsg.sendMessage(nvbMsg.constructNVBMsg(this.name, content));
        } 	
    }
}
