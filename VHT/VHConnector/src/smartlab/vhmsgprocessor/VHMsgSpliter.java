package smartlab.vhmsgprocessor;

import java.util.*;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import java.math.*;

// Split the messages that come from PSI
public class VHMsgSpliter {
	
	public String msgtype;
	public String identity;
	public String text;
	public String pose;
	public double coordinate_x;
	public double coordinate_y;
	public double coordinate_z;

	private static VHMsgSpliter instance = new VHMsgSpliter();
	public static VHMsgSpliter getInstance() {
		return instance;
	}

	private VHMsgSpliter() {
		System.out.println( "vhMsgSPliter Created!" );
	}
	
	//get the message type  (Multimodal = true-->NVBGmessage; multimodal= false -->text message)
	public String typeGetter (String s) {		
		if (s.contains("%;speech:")) {
			this.msgtype = "speech";
			return "speech";
		}
		else if (s.contains("%;location:")) {
			this.msgtype = "location";
			return "location";
		}
		else if (s.contains("%;pose:")) {
			this.msgtype = "pose";
			return "pose";
		}
		else {
			System.out.println("NO MATCH");
			return "NO MATCH TYPE";
		}
		
	}
	
	//get the identity information(Who).
	public String identityGetter (String s) {		
		Pattern pattern = Pattern.compile("(?<=;%;identity:).+?(?=;%;)");
		Matcher matcher = pattern.matcher(s);
		if (matcher.find()) {
			String identity = matcher.group(0);
			this.identity = identity;
			//System.out.println(identity);
			return identity;
		}
		else {
			System.out.println("NO MATCH");
			return "NO MATCH IDENTITY";
		}
		
	}
	
	//get the text information if this message is from bazaar.	
	public String textGetter (String s) {		
		Pattern pattern = Pattern.compile("(?<=;%;speech:)[\\s\\S]*$");
		Matcher matcher = pattern.matcher(s);
		if (matcher.find()) {
			String text = matcher.group(0);
			this.text = text;
			//System.out.println(identity);
			return text;
		}
		else {
			System.out.println("NO MATCH");
			return "NO MATCH TEXT";
		}		
	}
	
	//get the text information if this message is from bazaar.	
	public String poseGetter (String s) {		
		Pattern pattern = Pattern.compile("(?<=;%;pose:)[\\s\\S]*$");
		Matcher matcher = pattern.matcher(s);
		if (matcher.find()) {
			String pose = matcher.group(0);
			this.pose = pose;
			//System.out.println(identity);
			return pose;
		}
		else {
			System.out.println("NO MATCH");
			return "NO MATCH TEXT";
		}		
	}
	
	//get the coordinate information if this message is a multi-modal message.	
	public String[] coordinateGetter(String s) {
		Pattern pattern = Pattern.compile("(?<=;%;location:)[\\s\\S]*$");
		Matcher matcher = pattern.matcher(s);
		if (matcher.find()) {
			String xyz = matcher.group(0);
			//System.out.println(xyz+"000");
			String[] coordinate = xyz.split(":");		
			  //for(String out: coordinate) { System.out.println(out); }		 
			return coordinate;
		}
		else {
			System.out.println("NO MATCH COOR");
			return null;
		}
	}	
}
