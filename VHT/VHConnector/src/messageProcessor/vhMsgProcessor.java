package messageProcessor;

import java.util.*;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import java.math.*;

public class vhMsgProcessor {
	
	public String msgtype;
	public String identity;
	public String text;
	public double coordinate_x;
	public double coordinate_y;
	public double coordinate_z;
	//different message type
	public vhMsgProcessor() {
		this.msgtype = "text";
		this.identity = "someone";
		this.coordinate_x = 0;
		this.coordinate_y = 0;
		this.coordinate_z = 0;
		System.out.println( "vhMsgProcessor Created!" );
	}
	
	public String typeGetter (String s) {		
		Pattern pattern = Pattern.compile("(?<=message to Bazaar:multimodal:).+?(?=;%;)");
		Matcher matcher = pattern.matcher(s);
		if (matcher.find()) {
			String msgtype = matcher.group(0);
			this.msgtype = msgtype;
			//System.out.println(msgtype);
			return msgtype;
		}
		else {
			System.out.println("NO MATCH");
			return "NO MATCH TYPE";
		}
		
	}
	
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
	public double[] angleCalculate(String s) {
		String[] coordinate = coordinateGetter(s);
		double coordinate_x = Double.valueOf(coordinate[0].toString());
		double coordinate_y = Double.valueOf(coordinate[1].toString());
		double coordinate_z = Double.valueOf(coordinate[2].toString());
		double radianxy = Math.atan(coordinate_x/coordinate_y);
		double radianyz = Math.atan((coordinate_z-11.89)/Math.sqrt(Math.pow(coordinate_x, 2)+Math.pow(coordinate_y, 2)));
		double[] angle = {Math.toDegrees(radianxy), Math.toDegrees(radianyz), 55};
		/*
		 * for(double out: angle) { System.out.println(out); }
		 */
		return angle;
	}	

}
