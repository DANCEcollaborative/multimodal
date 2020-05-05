package vhMsgProcessor;

import java.util.*;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import java.math.*;

// Split the messages that come from PSI
public class VHMsgSpliter {
	
	public String msgtype;
	public String identity;
	public String text;
	public double coordinate_x;
	public double coordinate_y;
	public double coordinate_z;


	public VHMsgSpliter() {
		System.out.println( "vhMsgSPliter Created!" );
	}
	
	//get the message type  (Multimodal = true-->NVBGmessage; multimodal= false -->text message)
	public String typeGetter (String s) {		
		Pattern pattern = Pattern.compile("(?<=multimodal:).+?(?=;%;)");
		Matcher matcher = pattern.matcher(s);
		String msgtype = null;
		if (matcher.find()) {
			String tag = matcher.group(0);
			System.out.println(tag);
			if(tag.equals("true")) {
				msgtype = "NVB";
			}
			else if (tag.equals("false")) {
				msgtype = "text";
			}
			this.msgtype = msgtype;
			//System.out.println(msgtype);
			return msgtype;
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
		Pattern pattern = Pattern.compile("(?<=;%;text:)[\\s\\S]*$");
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
