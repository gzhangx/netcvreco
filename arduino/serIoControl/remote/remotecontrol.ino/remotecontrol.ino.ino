#include <Servo.h>
volatile int pwm_value = 0;
volatile int prev_time = 0;
int pin1Val = 0;

byte PWM_PIN1 = 13;
Servo servo1;

byte SERVO1 = 5;
void setup() {
  Serial.begin(115200);
  // when pin D2 goes high, call the rising function
  attachInterrupt(0, rising, RISING);
  pinMode(PWM_PIN1, INPUT);
  servo1.attach(SERVO1);
}
 
 
void rising() {
  attachInterrupt(0, falling, FALLING);
  prev_time = micros();
}
 
void falling() {
  attachInterrupt(0, rising, RISING);
  pwm_value = micros()-prev_time;
  Serial.println(pwm_value);
}

int servo1Value = 0;
void loop() {
  pin1Val = pulseIn(PWM_PIN1, HIGH);
  //Serial.println(pin1Val);
  //Serial.println(pwm_value);

  int newServo1Val = (int)(pin1Val/10*10);
  if (servo1Value != newServo1Val) {
    servo1.write(newServo1Val); 
    servo1Value = newServo1Val;
    Serial.println(newServo1Val);
  }
}
