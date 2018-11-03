/*
*/

#include <Servo.h>

Servo myservo;  // create servo object to control a servo
// twelve servo objects can be created on most boards

int pos = 90;    // variable to store the servo position
int speed = 0;
int driveCount = 0;
void setup() {
  Serial.begin(9600);
  while (!Serial) {
    ; // wait for serial port to connect. Needed for native USB port only
  }

  pinMode(13, OUTPUT);
  myservo.attach(9);  // attaches the servo on pin 9 to the servo object
}

byte serialBuffer[1024];
int serialBufferPos = 0;
int readSerialCommand(){
   if (Serial.available() > 0) {
    int rd = Serial.read();
    if (rd == 10 || serialBufferPos > 1000) {
     serialBuffer[serialBufferPos]= 0;
     int len = serialBufferPos;
     serialBufferPos = 0;
     return len;
    }
    serialBuffer[serialBufferPos++] = rd;
  }
  return 0;
}

void driveDebug(){
/*
  Serial.print(driveCount);
  Serial.print(" speed ");
  Serial.print(speed);
  Serial.print("\n");
  */
}
void drive() {
  
  int MAX = 5;

  if (driveCount>speed){
    driveDebug();
    digitalWrite(13, HIGH);
  }else {
    digitalWrite(13, LOW);
    driveDebug();
  }
  driveCount++;
  if (driveCount >= MAX) driveCount = 1;
}

int readIntVal() {
  String s = String((char*)serialBuffer+1);
  return s.toInt();
}
void loop() {
  if (readSerialCommand()> 0) {
    Serial.print("got cmd:");
    Serial.print(String((char*)serialBuffer));
    Serial.print("\n");
    char cmd = serialBuffer[0];
    if (cmd == 'R') {
       int spos = readIntVal();
       if (spos<0) spos = 0;
       else if (spos > 180) spos = 180;
       Serial.print("servo to ");
       Serial.print(spos);
       Serial.print("\n");
       myservo.write(spos); 
    }else if (cmd == 'D') {
      speed = readIntVal();
      Serial.print("got speed ");
      Serial.print(speed);
      Serial.print("\n");
      if (speed < 0) speed = 0;
      if (speed > 10) speed = 10;
    }
  }

  delay(15);
  drive();
}
