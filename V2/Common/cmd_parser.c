/*
  *****************************************************************************
  * @file    cmd_parser.c
  * @author  Y3288231
  * @date    Dec 16, 2014
  * @brief   This file contains functions for parsing commands
  ***************************************************************************** 
*/ 

// Includes ===================================================================
#include "cmsis_os.h"
#include "mcu_config.h"
#include "cmd_parser.h"
#include "commands.h"
#include "comms.h"
#include "scope.h"
#include "generator.h"
#include "adc.h"

// External variables definitions =============================================
xQueueHandle cmdParserMessageQueue;
command parseSystemCmd(void);
command parseCommsCmd(void);
command parseScopeCmd(void);
command parseGeneratorCmd(void);
command giveNextCmd(void);
void printErrResponse(command cmd);
// Function definitions =======================================================

/**
  * @brief  Command parser task function.
  * @param  Task handler, parameters pointer
  * @retval None
  */
	//portTASK_FUNCTION(vCmdParserTask, pvParameters) {
void CmdParserTask(void const *argument){
	portBASE_TYPE xHigherPriorityTaskWoken;
	cmdParserMessageQueue = xQueueCreate(10, 20);
	uint8_t message[20];
	uint8_t cmdIn[5];
	uint8_t chr;
	uint8_t byteRead;
	command tempCmd;
	while(1){
		xQueueReceive(cmdParserMessageQueue, message, portMAX_DELAY);
		///commsSendString("PARS_Run\r\n");

		if(message[0] == '1'){//parsing of command
			do{
				cmdIn[0] = cmdIn[1];
				cmdIn[1] = cmdIn[2];
				cmdIn[2] = cmdIn[3];
				cmdIn[3] = chr;
				byteRead = commBufferReadByte(&chr);
			}while(byteRead==0 && chr != ':' && chr != ';');
			
			if(byteRead==0){
				switch (BUILD_CMD(cmdIn)){
					case CMD_IDN: //send IDN
						xQueueSendToBack(messageQueue, STR_ACK, portMAX_DELAY);
						xQueueSendToBack (messageQueue, IDN_STRING, portMAX_DELAY);
					break;
					case CMD_SYSTEM: 
						tempCmd = parseSystemCmd();
						printErrResponse(tempCmd);
					break;
					case CMD_COMMS: 
						tempCmd = parseCommsCmd();
						printErrResponse(tempCmd);
					break;
					case CMD_SCOPE: //parse scope command
						tempCmd = parseScopeCmd();
						printErrResponse(tempCmd);
					break;
						
					case CMD_GENERATOR: //parse generator command
						tempCmd = parseGeneratorCmd();
						printErrResponse(tempCmd);
					break;
					default:
							xQueueSendToBack(messageQueue, UNSUPORTED_FUNCTION_ERR_STR, portMAX_DELAY);
				}	
			}
		}
		if (getBytesAvailable()>0){
			xQueueSendToBackFromISR(cmdParserMessageQueue, "1TryParseCmd", &xHigherPriorityTaskWoken);
		}
	}
}


/**
  * @brief  System command parse function 
  * @param  None
  * @retval Command ACK or ERR
  */
command parseSystemCmd(void){
	command cmdIn=CMD_ERR;
	uint8_t error=0;
	//try to parse command while buffer is not empty 

		do{ 
		cmdIn = giveNextCmd();
		switch(cmdIn){
			case CMD_GET_CONFIG:
				xQueueSendToBack(messageQueue, "3SendSystemConfig", portMAX_DELAY);
			break;
			case CMD_END:break;
			default:
				error = SYSTEM_INVALID_FEATURE;
				cmdIn = CMD_ERR;
			break;
		}
	}while(cmdIn != CMD_END && cmdIn != CMD_ERR && error==0);
	if(error>0){
		cmdIn=error;
	}
return cmdIn;
}

/**
  * @brief  Communications command parse function 
  * @param  None
  * @retval Command ACK or ERR
  */
command parseCommsCmd(void){
	command cmdIn=CMD_ERR;
	uint8_t error=0;
	//try to parse command while buffer is not empty 

		do{ 
		cmdIn = giveNextCmd();
		switch(cmdIn){
			case CMD_GET_CONFIG:
				xQueueSendToBack(messageQueue, "4SendCommsConfig", portMAX_DELAY);
			break;
			case CMD_END:break;
			default:
				error = COMMS_INVALID_FEATURE;
				cmdIn = CMD_ERR;
			break;
		}
	}while(cmdIn != CMD_END && cmdIn != CMD_ERR && error==0);
	if(error>0){
		cmdIn=error;
	}
return cmdIn;
}

/**
  * @brief  Scope command parse function 
  * @param  None
  * @retval Command ACK or ERR
  */
command parseScopeCmd(void){
	command cmdIn=CMD_ERR;
	uint8_t error=0;
	//try to parse command while buffer is not empty 

		do{ 
		cmdIn = giveNextCmd();
		switch(cmdIn){
			case CMD_SCOPE_TRIG_MODE://set trigger mode
				cmdIn = giveNextCmd();
				if(isScopeTrigMode(cmdIn)){
					if(cmdIn == CMD_MODE_NORMAL){
						scopeSetTriggerMode(TRIG_NORMAL);
					}else if(cmdIn == CMD_MODE_AUTO){
						scopeSetTriggerMode(TRIG_AUTO);
					}else if(cmdIn == CMD_MODE_SINGLE){
						scopeSetTriggerMode(TRIG_SINGLE);
					}
				}else{
					cmdIn = CMD_ERR;
					error = SCOPE_INVALID_FEATURE_PARAM;
				}
			break;
				
			case CMD_SCOPE_TRIG_EDGE: //set trigger edge
				cmdIn = giveNextCmd();
				if(isScopeTrigEdge(cmdIn)){
					if(cmdIn == CMD_EDGE_RISING){
						scopeSetTriggerEdge(EDGE_RISING);
					}else if(cmdIn == CMD_EDGE_FALLING){
						scopeSetTriggerEdge(EDGE_FALLING);
					}
				}else{
					cmdIn = CMD_ERR;
					error = SCOPE_INVALID_FEATURE_PARAM;
				}
			break;		
				
			case CMD_SCOPE_CHANNELS: //set number of channels
				cmdIn = giveNextCmd();
				if(isChannel(cmdIn)){
					if(cmdIn == CMD_CHANNELS_1){
						error=scopeSetNumOfChannels(1);
					}else if(cmdIn == CMD_CHANNELS_2){
						error=scopeSetNumOfChannels(2);
					}else if(cmdIn == CMD_CHANNELS_3){
						error=scopeSetNumOfChannels(3);
					}else if(cmdIn == CMD_CHANNELS_4){
						error=scopeSetNumOfChannels(4);
					}
				}else{
					cmdIn = CMD_ERR;
					error = SCOPE_INVALID_FEATURE_PARAM;
				}
			break;
				
			case CMD_SCOPE_TRIG_CHANNEL: //set trigger channel
				cmdIn = giveNextCmd();
				if(isChannel(cmdIn)){
					if(cmdIn == CMD_CHANNELS_1){
						error=scopeSetTrigChannel(1);
					}else if(cmdIn == CMD_CHANNELS_2){
						error=scopeSetTrigChannel(2);
					}else if(cmdIn == CMD_CHANNELS_3){
						error=scopeSetTrigChannel(3);
					}else if(cmdIn == CMD_CHANNELS_4){
						error=scopeSetTrigChannel(4);
					}
				}else{
					cmdIn = CMD_ERR;
					error = SCOPE_INVALID_FEATURE_PARAM;
				}
			break;
				
			case CMD_SCOPE_DATA_DEPTH: //set data bit depth
				cmdIn = giveNextCmd();
				if(isScopeDataDepth(cmdIn)){
					if(cmdIn == CMD_DATA_DEPTH_12B){
						error=scopeSetDataDepth(12);
					}else if(cmdIn == CMD_DATA_DEPTH_10B){
						error=SCOPE_UNSUPPORTED_RESOLUTION;
					}else if(cmdIn == CMD_DATA_DEPTH_8B){
						error=scopeSetDataDepth(8);
					}else if(cmdIn == CMD_DATA_DEPTH_6B){
						error=SCOPE_UNSUPPORTED_RESOLUTION;
					}
				}else{
					cmdIn = CMD_ERR;
					error = SCOPE_INVALID_FEATURE_PARAM;
				}
			break;
				
			case CMD_SCOPE_SAMPLING_FREQ: //set sampling frequency
				cmdIn = giveNextCmd();
				if(isScopeFreq(cmdIn)){
					if(cmdIn == CMD_FREQ_1K){
						error=scopeSetSamplingFreq(1000);
					}else if(cmdIn == CMD_FREQ_2K){
						error=scopeSetSamplingFreq(2000);
					}else if(cmdIn == CMD_FREQ_5K){
						error=scopeSetSamplingFreq(5000);
					}else if(cmdIn == CMD_FREQ_10K){
						error=scopeSetSamplingFreq(10000);
					}else if(cmdIn == CMD_FREQ_20K){
						error=scopeSetSamplingFreq(20000);
					}else if(cmdIn == CMD_FREQ_50K){
						error=scopeSetSamplingFreq(50000);
					}else if(cmdIn == CMD_FREQ_100K){
						error=scopeSetSamplingFreq(100000);
					}else if(cmdIn == CMD_FREQ_200K){
						error=scopeSetSamplingFreq(200000);
					}else if(cmdIn == CMD_FREQ_500K){
						error=scopeSetSamplingFreq(500000);
					}else if(cmdIn == CMD_FREQ_1M){
						error=scopeSetSamplingFreq(1000000);
					}else if(cmdIn == CMD_FREQ_2M){
						error=scopeSetSamplingFreq(2000000);
					}else if(cmdIn == CMD_FREQ_5M){
						error=scopeSetSamplingFreq(5000000);
					}else if(cmdIn == CMD_FREQ_10M){
						error=scopeSetSamplingFreq(10000000);
					}
				}else{
					cmdIn = CMD_ERR;
					error = SCOPE_INVALID_FEATURE_PARAM;
				}
			break;
						
			case CMD_SCOPE_TRIG_LEVEL: //set trigger level
				cmdIn = giveNextCmd();
				if(cmdIn != CMD_END && cmdIn != CMD_ERR){
					scopeSetTrigLevel((uint16_t)cmdIn);
				}else{
					cmdIn = CMD_ERR;
					error = SCOPE_INVALID_FEATURE_PARAM;
				}
			break;
				
			case CMD_SCOPE_PRETRIGGER: //set prettriger
				cmdIn = giveNextCmd();
				if(cmdIn != CMD_END && cmdIn != CMD_ERR){
					scopeSetPretrigger((uint16_t)cmdIn);
				}else{
					cmdIn = CMD_ERR;
					error = SCOPE_INVALID_FEATURE_PARAM;
				}
			break;	
				
			case CMD_SCOPE_DATA_LENGTH: //set trigger edge
				cmdIn = giveNextCmd();
				if(isScopeNumOfSamples(cmdIn)){
					if(cmdIn == CMD_SAMPLES_100){
						error=scopeSetNumOfSamples(100);
					}else if(cmdIn == CMD_SAMPLES_200){
						error=scopeSetNumOfSamples(200);
					}else if(cmdIn == CMD_SAMPLES_500){
						error=scopeSetNumOfSamples(500);
					}else if(cmdIn == CMD_SAMPLES_1K){
						error=scopeSetNumOfSamples(1000);
					}else if(cmdIn == CMD_SAMPLES_2K){
						error=scopeSetNumOfSamples(2000);
					}else if(cmdIn == CMD_SAMPLES_5K){
						error=scopeSetNumOfSamples(5000);
					}else if(cmdIn == CMD_SAMPLES_10K){
						error=scopeSetNumOfSamples(10000);
					}else if(cmdIn == CMD_SAMPLES_20K){
						error=scopeSetNumOfSamples(20000);
					}else if(cmdIn == CMD_SAMPLES_50K){
						error=scopeSetNumOfSamples(50000);
					}else if(cmdIn == CMD_SAMPLES_100K){
						error=scopeSetNumOfSamples(100000);
					}
				}else{
					cmdIn = CMD_ERR;
					error = SCOPE_INVALID_FEATURE_PARAM;
				}
			break;
				
			case CMD_SCOPE_START: //start sampling
				scopeStart();
			break;	
			
			case CMD_SCOPE_STOP: //stop sampling
				scopeStop();
			break;	
			
			case CMD_SCOPE_NEXT: //restart sampling
				scopeRestart();
			
			break;	
			case CMD_GET_CONFIG:
				xQueueSendToBack(messageQueue, "5SendScopeConfig", portMAX_DELAY);
			break;
				
			case CMD_END:break;
			default:
				error = SCOPE_INVALID_FEATURE;
				cmdIn = CMD_ERR;
			break;
		}
	}while(cmdIn != CMD_END && cmdIn != CMD_ERR && error==0);
	if(error>0){
		cmdIn=error;
	}
return cmdIn;
}





/**
  * @brief  Scope command parse function 
  * @param  None
  * @retval Command ACK or ERR
  */
command parseGeneratorCmd(void){
	command cmdIn=CMD_ERR;
	uint8_t error=0;
	uint16_t index;
	uint8_t length,chan;

		do{ 
		cmdIn = giveNextCmd();
		switch(cmdIn){
			case CMD_GEN_DATA://set data
				cmdIn = giveNextCmd();
				//index=(cmdIn&0xff00)>>8 | (cmdIn&0x00ff)<<8;
				index=SWAP_UINT16(cmdIn);
				length=cmdIn>>16;
				chan=cmdIn>>24;
				if(getBytesAvailable()<length*2+1){
					error=GEN_MISSING_DATA;
					while(commBufferReadByte(&chan)==0);
				}else{
					error=genSetData(index,length*2,chan);
					if (error){
						while(commBufferReadByte(&chan)==0);
					}
				}
			break;
				
			case CMD_GEN_SAMPLING_FREQ: //set sampling freq
				cmdIn = giveNextCmd();
				if(cmdIn != CMD_END && cmdIn != CMD_ERR){
					error=genSetFrequency(SWAP_UINT32(cmdIn)&0xffffff,(uint8_t)(cmdIn));
				}else{
					cmdIn = CMD_ERR;
				}
			break;	
				
			case CMD_GEN_GET_REAL_SMP_FREQ: //set sampling freq
				genSendRealSamplingFreq();
			break;	
				
			case CMD_GEN_DATA_LENGTH: //set data length
				cmdIn = giveNextCmd();
				if(cmdIn != CMD_END && cmdIn != CMD_ERR){
					error=genSetLength(SWAP_UINT32(cmdIn));
				}else{
					cmdIn = CMD_ERR;
				}
			break;	
			
			case CMD_GEN_CHANNELS: //set number of channels
				cmdIn = giveNextCmd();
				if(isChannel(cmdIn)){
					if(cmdIn == CMD_CHANNELS_1){
						error=genSetNumOfChannels(1);
					}else if(cmdIn == CMD_CHANNELS_2){
						error=genSetNumOfChannels(2);
					}else if(cmdIn == CMD_CHANNELS_3){
						error=genSetNumOfChannels(3);
					}else if(cmdIn == CMD_CHANNELS_4){
						error=genSetNumOfChannels(4);
					}
				}else{
					cmdIn = CMD_ERR;
				}
			break;
				
				
			case CMD_GEN_START: //start sampling
				genStart();
			break;	
			
			case CMD_GEN_STOP: //stop sampling
				genStop();
			break;	
			
			case CMD_GET_CONFIG:
				xQueueSendToBack(messageQueue, "6SendGenConfig", portMAX_DELAY);
			break;
				
			case CMD_END:break;
			default:
				error = GEN_INVALID_FEATURE;
				cmdIn = CMD_ERR;
			break;
		}
	}while(cmdIn != CMD_END && cmdIn != CMD_ERR && error==0);
	if(error>0){
		cmdIn=error;
	}
return cmdIn;
}

/**
  * @brief  Read command from input buffer 
  * @param  None
  * @retval Command
  */
command giveNextCmd(void){
	uint8_t cmdNext[5];
	uint8_t bytesRead = commBufferReadNBytes(cmdNext, 5);
	if(bytesRead >= 4){
		return BUILD_CMD(cmdNext);
	}else if(bytesRead == 0){
		return CMD_END;
	}else{
		return CMD_ERR;
	}
} 


/**
  * @brief  Printr error code 
  * @param  Command
  * @retval None
  */
void printErrResponse(command cmd){
	uint8_t err[5];
  if(cmd == CMD_END){
		xQueueSendToBack(messageQueue, STR_ACK, portMAX_DELAY);
	}else{
		err[0]=ERROR_PREFIX;
		err[1]=(cmd/100)%10+48;
		err[2]=(cmd/10)%10+48;
		err[3]=cmd%10+48;
		err[4]=0;
		xQueueSendToBack(messageQueue, err, portMAX_DELAY);
	}
}
