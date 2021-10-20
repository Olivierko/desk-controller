// pins
#define PIN_SIGNAL_IN 2
#define PIN_UP_OUT 4
#define PIN_DOWN_OUT 5

#define CYCLE_RATE 1000
#define MESSAGE_LENGTH 2

// message marks
#define MSG_START_MARK 0x11
#define MSG_END_MARK 0x12

// message types
#define MSG_STATUS_REQUEST 0x21
#define MSG_STATUS_RESPONSE 0x22
#define MSG_DEBUG_REQUEST 0x23
#define MSG_DEBUG_RESPONSE 0x24
#define MSG_STOP_REQUEST 0x25
#define MSG_DOWN_REQUEST 0x27
#define MSG_UP_REQUEST 0x29

enum Direction {
  none = 0x00,
  down = 0x01,
  up = 0x02
};

unsigned int _serialInput;
unsigned long _lastTime;
Direction _state;

void setup(){
  _state = none;

  Serial.begin(9600);

  pinMode(LED_BUILTIN, OUTPUT);
  pinMode(PIN_UP_OUT, OUTPUT);
  pinMode(PIN_DOWN_OUT, OUTPUT);
  pinMode(PIN_SIGNAL_IN, INPUT);

  attachInterrupt(digitalPinToInterrupt(PIN_SIGNAL_IN), onSignalInChange, CHANGE);
}

bool checkTime(){
  unsigned long currentTime = micros();
  if (currentTime - _lastTime < 1000){
    return false;
  }
  _lastTime = currentTime;
  return true;
}

void onSignalInChange(){
  _lastTime = micros() - (CYCLE_RATE / 2);
}

void sendMessage(byte bytes[]){
  Serial.write(MSG_START_MARK);
  for(int i=0; i<MESSAGE_LENGTH; ++i){
    Serial.write(bytes[i]);
  }
  Serial.write(MSG_END_MARK);
}

void handleMessaging(){
  if (!Serial.available()){
    return;
  }

  _serialInput = Serial.read();

  switch (_serialInput)
  {
    case MSG_STATUS_REQUEST: {
        byte msg[] = {MSG_STATUS_RESPONSE, _state};
        sendMessage(msg);
      }
      break;
    case MSG_STOP_REQUEST:
      _state = none;
      break;
    case MSG_DOWN_REQUEST:
      _state = (_state == none) ? down : none;
      break;
    case MSG_UP_REQUEST:
      _state = (_state == none) ? up : none;
      break;
    case MSG_DEBUG_REQUEST:
      digitalWrite(PIN_SIGNAL_IN, LOW);
      delay(50);
      break;
  }
}

void handleState(){
  switch (_state) {
    case none:
      digitalWrite(LED_BUILTIN, LOW);
      digitalWrite(PIN_UP_OUT, LOW);
      digitalWrite(PIN_DOWN_OUT, LOW);
      break;
    case down:
      digitalWrite(PIN_UP_OUT, LOW);
      digitalWrite(LED_BUILTIN, HIGH);
      digitalWrite(PIN_DOWN_OUT, HIGH);
      break;
    case up:
      digitalWrite(PIN_DOWN_OUT, LOW);
      digitalWrite(LED_BUILTIN, HIGH);
      digitalWrite(PIN_UP_OUT, HIGH);
      break;
  }
}

void handleSignal(){
    unsigned char signalValue = digitalRead(PIN_SIGNAL_IN);

    byte msg[] = {MSG_DEBUG_RESPONSE, signalValue};
    sendMessage(msg);
}

void loop(){

  if (checkTime()){
    handleSignal();
  }

  handleMessaging();
  handleState();
}