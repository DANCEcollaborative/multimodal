package VHjava;
import java.io.*;
import edu.usc.ict.vhmsg.*;

public class VHCharacterSender {

    public static VHMsg vhmsg;

    public int numMessagesReceived = 0;
    public int m_testSpecialCases = 0;
    public String name = "Rachel";

    public void setName(String name) {
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

    public VHCharacterSender()
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

    private String constructVrSpeech(String s) {
        if (this.name.equals("Rachel")) {
            return "vrExpress Rachel User user0003-1570425438621-56-1 <?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\" ?>\n" +
                    "                      <act>\n" +
                    "                       <participant id=\"Brad\" role=\"actor\" />\n" +
                    "                       <fml>\n" +
                    "                       <turn start=\"take\" end=\"give\" />\n" +
                    "                       <affect type=\"neutral\" target=\"addressee\"></affect>\n" +
                    "                       <culture type=\"neutral\"></culture>\n" +
                    "                       <personality type=\"neutral\"></personality>\n" +
                    "                       </fml>\n" +
                    "                       <bml>\n" +
                    s +
                    "                       </bml>\n" +
                    "                      </act>\n";
        }
        else if (this.name.equals("Brad")) {
            return "vrExpress Brad all 1986376508 <?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\" ?>\n" +
                    "                      <act>\n" +
                    "                       <participant id=\"Brad\" role=\"actor\" />\n" +
                    "                       <fml>\n" +
                    "                       <turn start=\"take\" end=\"give\" />\n" +
                    "                       <affect type=\"neutral\" target=\"addressee\"></affect>\n" +
                    "                       <culture type=\"neutral\"></culture>\n" +
                    "                       <personality type=\"neutral\"></personality>\n" +
                    "                       </fml>\n" +
                    "                       <bml>\n" +
                    s +
                    "</bml>\n" +
                    "</act>\n";
        }
        return "";
    }



    public void sendMessage(String s) {
        vhmsg.sendMessage(constructVrSpeech(s));
    }
}
