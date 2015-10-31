/*
  *****************************************************************************
  * @file    comms.c
  * @author  Y3288231
  * @date    Dec 15, 2014
  * @brief   This file contains functions for communication
  ***************************************************************************** 
*/ 

// Includes ===================================================================

#include "cmsis_os.h"
#include "mcu_config.h"
#include "comms.h"
#include "comms_hal.h"
#include "cmd_parser.h"
#include "scope.h"
#include "generator.h"
#include "commands.h"



// External variables definitions =============================================
xQueueHandle messageQueue;
static xSemaphoreHandle commsMutex ;
static uint8_t commBuffMem[COMM_BUFFER_SIZE];
static commBuffer comm;

void sendSystConf(void);
void sendCommsConf(void);
void sendScopeConf(void);
void sendGenConf(void);

// Function definitions =======================================================
/**
  * @brief  Communication task function.
  * @param  Task handler, parameters pointer
  * @retval None
  */
//portTASK_FUNCTION(vPrintTask, pvParameters) {
void CommTask(void const *argument){
	commsInit();
	messageQueue = xQueueCreate(5, 30);
	commsMutex = xSemaphoreCreateRecursiveMutex();
	char message[30];
	uint8_t header[16]="OSC_DATAxxxxCH0x";
	uint8_t header_gen[12]="GEN_xCH_Fxxx";
	uint8_t *pointer;
	uint8_t i;
	uint32_t j;
	uint8_t k;
	uint32_t tmpToSend;
	uint32_t dataLength;
	uint32_t dataLenFirst;
	uint32_t dataLenSecond;
	uint16_t adcRes;
	uint32_t oneChanMemSize;
	
	while(1) {	
		xQueueReceive (messageQueue, message, portMAX_DELAY);
		///commsSendString("COMMS_Run\r\n");
		xSemaphoreTakeRecursive(commsMutex, portMAX_DELAY);
		GPIOD->ODR |= GPIO_PIN_14;
		//send data
		if(message[0]=='1' && getScopeState() == SCOPE_DATA_SENDING){
			/////TODO - j is pointer where to start send data. Do correct sending ;)
			oneChanMemSize=getOneChanMemSize();
			dataLength = getSamples();
			adcRes = getADCRes();
			
			if(adcRes>8){
				j = ((getTriggerIndex() - ((getSamples() * getPretrigger()) >> 16 ))*2+oneChanMemSize)%oneChanMemSize;
				dataLength*=2;
			}else{
				j = ((getTriggerIndex() - ((getSamples() * getPretrigger()) >> 16 ))+oneChanMemSize)%oneChanMemSize;
			} 
			
			header[8]=(uint8_t)adcRes;
			header[9]=(uint8_t)(dataLength >> 16);
			header[10]=(uint8_t)(dataLength >> 8);
			header[11]=(uint8_t)dataLength;
			header[15]=GetNumOfChannels();
			
			if(j+dataLength>oneChanMemSize){
				dataLenFirst=oneChanMemSize-j;
				dataLenSecond=dataLength-dataLenFirst;
			}else{
				dataLenFirst=dataLength;
				dataLenSecond=0;
			}
			
			for(i=0;i<GetNumOfChannels();i++){
			
				pointer = (uint8_t*)getDataPointer(i);
			
				//sending header
				header[14]=(i+1);
				
				commsSendBuff(header,16);
				
				if(dataLenFirst>16384 ){
					tmpToSend=dataLenFirst;
					k=0;
					while(tmpToSend>16384){
						commsSendBuff(pointer + j+k*16384, 16384);
						k++;
						tmpToSend-=16384;
					}
					if(tmpToSend>0){
					commsSendBuff(pointer + j+k*16384, tmpToSend);
					}
				}else if(dataLenFirst>0){
					commsSendBuff(pointer + j, dataLenFirst);
				}
				
				if(dataLenSecond>16384 ){
					tmpToSend=dataLenSecond;
					k=0;
					while(tmpToSend>16384){
						commsSendBuff(pointer+k*16384, 16384);
						k++;
						tmpToSend-=16384;
					}
					if(tmpToSend>0){
					commsSendBuff(pointer+k*16384, tmpToSend);
					}
				}else if(dataLenSecond>0){
					commsSendBuff(pointer, dataLenSecond);
				}
			}	
			///commsSendString("COMMS_DataSending\r\n");
			commsSendString(STR_SCOPE_OK);
			xQueueSendToBack(scopeMessageQueue, "2DataSent", portMAX_DELAY);
			
		//send generating frequency	
		}else if(message[0]=='2'){
			for(i = 0;i<MAX_DAC_CHANNELS;i++){
				header_gen[4]=i+1+48;
				j=getRealSmplFreq(i+1);
				header_gen[9]=(uint8_t)(j>>16);
				header_gen[10]=(uint8_t)(j>>8);
				header_gen[11]=(uint8_t)(j);
				commsSendBuff(header_gen,12);
			}
		// send system config
		}else if(message[0]=='3'){
			sendSystConf();
			
		// send comms config
		}else if(message[0]=='4'){
			sendCommsConf();
			
		// send scope config
		}else if(message[0]=='5'){
			sendScopeConf();
			
		// send gen config
		}else if(message[0]=='6'){
			sendGenConf();
			
		// send gen next data block
		}else if(message[0]=='7'){
			commsSendString(STR_GEN_NEXT);
			
		// send gen ok status
		}else if(message[0]=='8'){
			commsSendString(STR_GEN_OK);
			
		// send IDN string
		}else if (message[0] == 'I'){
			xQueueReceive(messageQueue, message, portMAX_DELAY);
			commsSendString(message);
			/////commsSendString("\r\n");
			
		// not known message -> send it
		}else{
			commsSendString(message);
			/////commsSendString("\r\n");
		}
		GPIOD->ODR &= ~GPIO_PIN_14;
		xSemaphoreGiveRecursive(commsMutex);
	}
}

/**
  * @brief  Communication initialisation.
  * @param  None
  * @retval None
  */
void commsInit(void){
	//commHalInit();
	MX_USB_DEVICE_Init();
	comm.memory = commBuffMem;
	comm.bufferSize = COMM_BUFFER_SIZE;
	comm.writePointer = 0;
	comm.readPointer = 0;
	comm.state = BUFF_EMPTY;
}

/**
  * @brief  Store incoming byte to buffer
  * @param  incoming byte
  * @retval 0 success, 1 error - buffer full
  */
uint8_t commBufferStoreByte(uint8_t chr){
	if(comm.state == BUFF_FULL){
		return 1;
	}else{
		*(comm.memory + comm.writePointer) = chr;
		comm.writePointer = (comm.writePointer + 1) % COMM_BUFFER_SIZE;
		if(comm.state == BUFF_EMPTY){
			comm.state = BUFF_DATA;
		}else if(comm.state == BUFF_DATA && comm.writePointer == comm.readPointer){
			comm.state = BUFF_FULL;
		}
		return 0;
	}
}

/**
  * @brief  Read byte from coms buffer
  * @param  pointer where byte will be written
  * @retval 0 success, 1 error - buffer empty
  */
uint8_t commBufferReadByte(uint8_t *ret){
	if(comm.state == BUFF_EMPTY){
		return 1;
	}else{
		*ret = *(comm.memory + comm.readPointer);
		comm.readPointer = (comm.readPointer + 1) % COMM_BUFFER_SIZE;
		if(comm.state == BUFF_FULL){
			comm.state = BUFF_DATA;
		}else if(comm.state == BUFF_DATA && comm.writePointer == comm.readPointer){
			comm.state = BUFF_EMPTY;
		}
		return 0;
	}
}

/**
  * @brief  Read N bytes from coms buffer
  * @param  pointer where bytes will be written and number of bytes to read
  * @retval Number of bytes read
  */
uint8_t commBufferReadNBytes(uint8_t *mem, uint16_t count){
	for(uint16_t i = 0; i < count; i++){
		if(commBufferReadByte(mem++) == 1){
			return i;
		}
	}
	return count;
}

/**
  * @brief  Read N bytes from coms buffer
  * @param  pointer where bytes will be written and number of bytes to read
  * @retval Number of bytes read
  */
uint16_t commBufferLookNewBytes(uint8_t *mem){
	uint16_t result = commBufferCounter();
	for(uint16_t i = 0;i<result;i++){
		*(mem++)=*(comm.memory+((comm.readPointer+i)%COMM_BUFFER_SIZE));
	}
	return result;
}



/**
  * @brief  Read N bytes from coms buffer
  * @param  pointer where bytes will be written and number of bytes to read
  * @retval Number of bytes read
  */
uint16_t commBufferCounter(void){
	if(comm.state == BUFF_FULL){
		return COMM_BUFFER_SIZE;
	}else{
		return (comm.writePointer+COMM_BUFFER_SIZE-comm.readPointer)%COMM_BUFFER_SIZE;
	}
}

/**
  * @brief  Processing of incoming byte
  * @param  incomming byte
  * @retval 0 success, 1 error - buffer full
  */
uint8_t commInputByte(uint8_t chr){
	portBASE_TYPE xHigherPriorityTaskWoken;
	uint8_t result=0;
	if (chr==';'){
		result = commBufferStoreByte(chr);
		xQueueSendToBackFromISR(cmdParserMessageQueue, "1TryParseCmd", &xHigherPriorityTaskWoken);
		return result;
	}else{
		return commBufferStoreByte(chr);
	}
}


uint16_t getBytesAvailable(){
	uint16_t result; 
	if(comm.state==BUFF_FULL){
		return COMM_BUFFER_SIZE;
	}else if(comm.state==BUFF_EMPTY){
		return 0;
	}else{
		result = (comm.writePointer+COMM_BUFFER_SIZE-comm.readPointer)%COMM_BUFFER_SIZE;
		return result;
	}
}


void sendSystConf(){
	commsSendString("SYST");
	commsSendUint32(HAL_RCC_GetHCLKFreq());  //CCLK
	commsSendUint32(HAL_RCC_GetPCLK2Freq()); //PCLK
	commsSendString(MCU);
}

void sendCommsConf(){
	commsSendString("COMM");
	commsSendUint32(COMM_BUFFER_SIZE);
	commsSendUint32(UART_SPEED);
	commsSendString(USART_TX_PIN_STR);
	commsSendString(USART_RX_PIN_STR);
	#ifdef USE_USB
	commsSendString("USB_");
	commsSendString(USB_DP_PIN_STR);
	commsSendString(USB_DM_PIN_STR);
	#endif
}

void sendScopeConf(){
	uint8_t i;
	commsSendString("OSCP");
	commsSendUint32(MAX_SAMPLING_FREQ);
	commsSendUint32(MAX_SCOPE_BUFF_SIZE);
	commsSendUint32(MAX_ADC_CHANNELS);
	for (i=0;i<MAX_ADC_CHANNELS;i++){
		switch(i){
			case 0:
				commsSendString(SCOPE_CH1_PIN_STR);
				break;
			case 1:
				commsSendString(SCOPE_CH2_PIN_STR);
				break;
			case 2:
				commsSendString(SCOPE_CH3_PIN_STR);
				break;
			case 3:
				commsSendString(SCOPE_CH4_PIN_STR);
				break;
		}
	}
	commsSendUint32(SCOPE_VREF);
	commsSendBuff((uint8_t*)scopeGetRanges(&i),i);

}

void sendGenConf(){
	uint8_t i;
	commsSendString("GEN_");
	commsSendUint32(MAX_GENERATING_FREQ);
	commsSendUint32(MAX_GENERATOR_BUFF_SIZE);
	commsSendUint32(DAC_DATA_DEPTH);
	commsSendUint32(MAX_DAC_CHANNELS);
	for (i=0;i<MAX_DAC_CHANNELS;i++){
		switch(i){
			case 0:
				commsSendString(GEN_CH1_PIN_STR);
				break;
			case 1:
				commsSendString(GEN_CH2_PIN_STR);
				break;
		}
	}
	commsSendUint32(GEN_VREF);
}
	





