package smartlab.vhmsgprocessor;

import java.util.*;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

import smartlab.vhmsgprocessor.VHMsgSpliter;

import java.math.*;

/*
 * Processing the non-verbal behavior (NVB) that revieved from PSI
 */
public class NVBMsgProcessor {

	public String msgtype;
	public String identity;
	public String[] Coordinate;
	VHMsgSpliter vhmsgspliter = VHMsgSpliter.getInstance();
	
	//initialization 
	public NVBMsgProcessor() {
		System.out.println( "NVBMsgProcessor Created!" );
	}
	
	/*
	 * @param content :String
	 * content of received the message
	 */
	public NVBMsgProcessor(String content) {
		this.msgtype = vhmsgspliter.typeGetter(content);
		this.identity = vhmsgspliter.identityGetter(content);
		this.Coordinate = vhmsgspliter.coordinateGetter(content);
		System.out.println( "NVBMsgProcessor Created!" );
	}
	
	/*
	 * Get the position of the gaze
	 * @param content :String
	 * content of received the message
	 */
	public String angleGetter(String content) {
		double[] rowangle = angleCalculate(content);
		String angle = String.valueOf(rowangle[0])+" "+String.valueOf(rowangle[1])+" "+String.valueOf(rowangle[2]);
		return angle;
	}
	
    //send the NVB message(GAZE) from Bazaar to VHT through sbm
    public String constructNVBMsg(String name, String content) {
    	String s = angleGetter(content);
        if (name.equals("Rachel")) {
            return "sbm bml char Rachel <gaze sbm:target-pos=\""+s+"\" sbm:joint-range=\"EYES NECK CHEST\"/>" ;
        }
        else if (name.equals("Brad")) {
            return "sbm bml char Brad <gaze sbm:target-pos=\""+s+"\" sbm:joint-range=\"EYES NECK CHEST\"/>";
        }
        return "Wrong GazaMsg!";
    	
    }
	
    //calculate the gaze position. Now it is the value that PSI give directly.  
	private double[] angleCalculate(String content) {
		double x = Double.valueOf(vhmsgspliter.coordinateGetter(content)[0].toString());
		double y = Double.valueOf(vhmsgspliter.coordinateGetter(content)[1].toString());
		double z = Double.valueOf(vhmsgspliter.coordinateGetter(content)[2].toString());
		//double radianxy = Math.atan(coordinate_x/coordinate_y);
		//double radianyz = Math.atan((coordinate_z-11.89)/Math.sqrt(Math.pow(coordinate_x, 2)+Math.pow(coordinate_y, 2)));
		double[] angle = {x, y, z};
		/*
		 * for(double out: angle) { System.out.println(out); }
		 */
		return angle;
	}
}
