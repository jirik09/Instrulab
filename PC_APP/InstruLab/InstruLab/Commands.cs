using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InstruLab
{
    class Commands
    {
        public const string IDN = "IDN_";

        /* KOMUNIKACE */
        // obecne
        public const string IDNRequest = "IDN?";
        public const string CONFIGRequest = "CFG?";

        public const string GENERATOR = "GEN_";
        public const string SCOPE = "OSCP";
        public const string COMMS = "COMS";
        public const string SYSTEM = "SYST";

        public const string ACKNOWLEDGE = "ACK_";


        /*
         REGISTER_CMD(IDN,IDN?),
REGISTER_CMD(GET_CONFIG,CFG?),

REGISTER_CMD(SCOPE,OSCP),
REGISTER_CMD(GENERATOR,GEN_),
REGISTER_CMD(COMMS,COMS),
REGISTER_CMD(SYSTEM,SYST),	
	
REGISTER_CMD(ERR,ERR_),
REGISTER_CMD(ACK,ACK_),
REGISTER_CMD(NACK,NACK),
REGISTER_CMD(END,END_),
         
         */

    }
}
