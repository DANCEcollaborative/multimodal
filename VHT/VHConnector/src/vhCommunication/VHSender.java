package vhCommunication;
import java.io.*;
import edu.usc.ict.vhmsg.*;

// Send the VHmsg as needed,both text and NVB message.
public class VHSender {

    public static VHMsg vhmsg;
    public String name = "Brad";
    public String msgtype = "text";

    public int numMessagesReceived = 0;
    public int m_testSpecialCases = 0;

    public void setChar(String name) {
        this.name = name;
    }
    
    public void setMsgType(String msgtype) {
        this.msgtype = msgtype;
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

    //send the text message from Bazaar to VHT through vrExpress
    private String constructTextMsg(String name, String s) {
        if (name.equals("Rachel")) {
            return  "vrExpress Rachel User user0003-1570425438621-56-1 <?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\" ?>\n" +
                    "                      <act>\n" +
                    "                       <participant id=\"Rachel\" role=\"actor\" />\n" +
                    "                       <fml>\n" +
                    "                       <turn start=\"take\" end=\"give\" />\n" +
                    "                       <affect type=\"neutral\" target=\"addressee\"></affect>\n" +
                    "                       <culture type=\"neutral\"></culture>\n" +
                    "                       <personality type=\"neutral\"></personality>\n" +
                    "                       </fml>\n" +
                    "                       <bml>\n" +
                                               "<speech id=\"sp1\" ref=\"rachel_ownvoiceTTS\" type=\"application/ssml+xml\">\n"+
                                                s +
                                                "</speech>\r\n"+
                    "                       </bml>\n" +
                    "                      </act>\n";
        }
        else if (name.equals("Brad")) {
            return  "vrExpress Brad User user0003-1570425438621-56-1 <?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\" ?>\n" +
                    "                      <act>\n" +
                    "                       <participant id=\"Brad\" role=\"actor\" />\n" +
                    "                       <fml>\n" +
                    "                       <turn start=\"take\" end=\"give\" />\n" +
                    "                       <affect type=\"neutral\" target=\"addressee\"></affect>\n" +
                    "                       <culture type=\"neutral\"></culture>\n" +
                    "                       <personality type=\"neutral\"></personality>\n" +
                    "                       </fml>\n" +
                    "                       <bml>\n" +
                    "<speech id=\"sp1\" type=\"application/ssml+xml\">\n"+
                    s +
                    "</speech>\r\n"+
                    "                       </bml>\n" +
                    "                      </act>\n";
        }
        return "Wrong TextMsg!";
    	
    }
    
    //send the NVB message(GAZE) from Bazaar to VHT through sbm
    private String constructGazeMsg(String name, String s) {
        if (name.equals("Rachel")) {
            return "sbm bml char Rachel <gaze sbm:target-pos=\""+s+"\" sbm:joint-range=\"EYES NECK CHEST\"/>" ;
        }
        else if (name.equals("Brad")) {
            return "sbm bml char Brad <gaze sbm:target-pos=\""+s+"\" sbm:joint-range=\"EYES NECK CHEST\"/>";
        }
        return "Wrong GazaMsg!";
    	
    }
    
  //send the NVB message(nod) from Bazaar to VHT through sbm
    private String constructNodMsg(String name, String s) {
        return "Nod-Not written yet";    	
    }
  //send the NVB message(head) from Bazaar to VHT through sbm
    private String constructHeadMsg(String name, String s) {
        return "Head-Not written yet";    	
    }
    
    //this is a test method
	/*    private String constructVrSpeech(String name, String s) {
	    if (name.equals("Rachel")) {
	        return s;
	    }
	    else if (name.equals("Brad")) {
	        return s;
	    }
	    return "Wrong VrSpeechMsg!";
	}*/

    public void sendMessage(String name, String s, String msgtype) {
        if (msgtype.equals("false")) {
        	vhmsg.sendMessage(constructTextMsg(name, s));
        }
        else if (msgtype.equals("true")) {
        	vhmsg.sendMessage(constructGazeMsg(name, s));
        }    	
    }
    
    public void sendMessage(String s, String msgtype) {
        if (msgtype.equals("false")) {
        	vhmsg.sendMessage(constructTextMsg(this.name, s));
        }
        else if (msgtype.equals("true")) {
        	vhmsg.sendMessage(constructGazeMsg(this.name, s));
        }    	
    }

    //default is sending the text message
    public void sendMessage(String s) {vhmsg.sendMessage(constructTextMsg(this.name, s));}
}
