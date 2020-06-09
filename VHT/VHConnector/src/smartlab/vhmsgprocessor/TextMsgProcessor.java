package smartlab.vhmsgprocessor;

/*
 * Processing the text message that revieved from PSI
 */
public class TextMsgProcessor {
	VHMsgSpliter vhmsgspliter = VHMsgSpliter.getInstance();

	public TextMsgProcessor() {
		super();
		System.out.println( "TextMsgProcessor Created!" );
		// TODO Auto-generated constructor stub
	}
	
    //send the text message from Bazaar to VHT through VrExpress
    public String constructTextMsg(String name, String content) {
    	String s = vhmsgspliter.textGetter(content);
    	System.out.println("!!!!Messages to Rachel!!!!"+s);
    	//String s = content;
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
	

}
