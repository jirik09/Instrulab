/*
  *****************************************************************************
  * @file    err_list.h
  * @author  Y3288231
  * @date    Sep, 2015
  * @brief   This file contains list of errors
  ***************************************************************************** 
*/ 
#ifndef ERR_LIST_H_
#define ERR_LIST_H_

// List of possible Errors ====================================================
#define BUFFER_SIZE_ERR 8 // Buffer size exceeded located memory
#define SCOPE_INVALID_FEATURE 2
#define GEN_INVALID_FEATURE 3
#define SCOPE_INVALID_FEATURE_PARAM 4
#define SCOPE_UNSUPPORTED_RESOLUTION 5
#define SCOPE_INVALID_TRIGGER_CHANNEL 6
#define SCOPE_INVALID_SAMPLING_FREQ 7

#define UNSUPORTED_FUNCTION_ERR_STR "E999" // Unsupported function
#define UNKNOW_ERROR 100


#endif /* ERR_LIST_H_ */

