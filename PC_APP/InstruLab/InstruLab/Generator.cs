﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Timers;
using ZedGraph;
using System.IO;

namespace InstruLab
{
    public partial class Generator : Form
    {
        //Thread gen_th;
        Device device;
        System.Timers.Timer signalTimer;
        System.Timers.Timer dataSendingTimer;

        static Semaphore genSemaphore = new Semaphore(1, 1);  // Dostupná kapacita=1; Celková=1

        int semaphoreTimeout = 5000;

        private enum SIGNAL_TYPE { SINE,SQUARE,SAW,ARB };


        private bool bestFreqFit = true;
        private bool customLeng = false;

        private bool frequencyJoin = false;

        private bool khz_ch1=false;
        private bool khz_ch2 = false;
        private double freq_ch1 = 0; 
        private double freq_ch2 = 0;
        private double ampl_ch1 = 0;
        private double ampl_ch2 = 0;
        private double phase_ch1 = 0;
        private double phase_ch2 = 0;
        private double duty_ch1 = 50;
        private double duty_ch2 = 50;
        private double offset_ch1 = 0;
        private double offset_ch2 = 0;

        private int signal_leng_ch1 = 0;
        private int signal_leng_ch2 = 0;

        private int signal_leng = 0;

        private double last_sum = 0;
        private int divider_ch1 = 0;
        private int divider_ch2 = 0;

        private SIGNAL_TYPE signalType_ch1 = SIGNAL_TYPE.SINE;
        private SIGNAL_TYPE signalType_ch2 = SIGNAL_TYPE.SINE;
        private double[] signal_ch1; 
        private double[] signal_ch2;

        private double[] arb_signal_ch1 = new double[468] { 0.467352832186278, 0.468329915210844, 0.471482988167838, 0.476615160619296, 0.483392527677368, 0.491365799742661, 0.500000000000000, 0.508710272506303, 0.516901409885865, 0.524008435097523, 0.529535476540453, 0.533090267963558, 0.534411879587437, 0.533389727402142, 0.530072485204825, 0.524666200442660, 0.517521645111034, 0.509111667552676, 0.500000000000000, 0.490803572749159, 0.482150847503713, 0.474638977009659, 0.468792704032729, 0.465027821062211, 0.463621727293281, 0.464693159062807, 0.468192563725666, 0.473903874893997, 0.481457676532983, 0.490354966541801, 0.500000000000000, 0.509740058418378, 0.518909498189136, 0.526875113915138, 0.533079733725308, 0.537081054049615, 0.538583016507126, 0.537457511451134, 0.533754830331946, 0.527702040497142, 0.519689271310338, 0.510244724657672, 0.500000000000000, 0.489648000738582, 0.479896217714708, 0.471418529645806, 0.464808793909247, 0.460539413070196, 0.458927756621446, 0.460112812130414, 0.464043767689883, 0.470481432257144, 0.479012534976893, 0.489076067077731, 0.500000000000000, 0.511045988038719, 0.521459093450593, 0.530519197157868, 0.537590606506031, 0.542166455747848, 0.543904811887420, 0.542653929224702, 0.538464806657334, 0.531590046180951, 0.522468933142385, 0.511699596798407, 0.500000000000000, 0.488160288269876, 0.476989646781895, 0.467261224867014, 0.459658861310601, 0.454729265301390, 0.452842979824624, 0.454166895926252, 0.458650332843366, 0.466025799390298, 0.475824565606294, 0.487406166503626, 0.500000000000000, 0.512756334573747, 0.524803367754581, 0.535306522202239, 0.543525965428036, 0.548868408979294, 0.550929581789407, 0.549524360777674, 0.544702342872037, 0.536747604741106, 0.526162456398668, 0.513636081785729, 0.500000000000000, 0.486173203783701, 0.473100572998553, 0.461688667397570, 0.452743237535275, 0.446912735569256, 0.444641758924558, 0.446137739008224, 0.451353332756901, 0.459985941504129, 0.471494637058168, 0.485133595045633, 0.500000000000000, 0.515093372968937, 0.529382451032350, 0.541875177495680, 0.551687083945793, 0.558103383904515, 0.560630454511198, 0.559033038046987, 0.553354409234367, 0.543917869080835, 0.531309169132832, 0.516340759164717, 0.500000000000000, 0.483384606227473, 0.467629503099953, 0.453829932504764, 0.442965976335677, 0.435833654296753, 0.432987392382360, 0.434697966762183, 0.440929046919094, 0.451334253180697, 0.465275285143587, 0.481860258174947, 0.500000000000000, 0.518478802419913, 0.536035081454769, 0.551446646637549, 0.563614872548668, 0.571642036464790, 0.574896443807951, 0.573060690652212, 0.566159467450615, 0.554564625221643, 0.538976720757199, 0.520383833597224, 0.500000000000000, 0.479187033063887, 0.459364695380793, 0.441915076376961, 0.428087535379766, 0.418910662023369, 0.415117363684323, 0.417088429709288, 0.424818786987937, 0.437909219575372, 0.455584667044122, 0.476738448718462, 0.500000000000000, 0.523822070589527, 0.546581934563482, 0.566690097493119, 0.582699334313269, 0.593406705770549, 0.597941503441166, 0.595832853972382, 0.587051930856073, 0.572025305292569, 0.551617819381155, 0.527085367930558, 0.500000000000000, 0.472151663958723, 0.445432590939922, 0.421711624681991, 0.402706665513801, 0.389863734986965, 0.384250950478622, 0.386474926832717, 0.396625832108414, 0.414255588937418, 0.438391634932169, 0.467586362968348, 0.500000000000000, 0.533512404388656, 0.565857217831129, 0.594770138542853, 0.618141906161812, 0.634165995561334, 0.641471060526129, 0.639228863318366, 0.627229745097337, 0.605919566606719, 0.576394372684110, 0.540351670590423, 0.500000000000000, 0.457931237044028, 0.416962638386838, 0.379957824512386, 0.349637573975875, 0.328392331258759, 0.318108636466405, 0.320021225466503, 0.334601331373462, 0.361489797514291, 0.399481088573539, 0.446561301109979, 0.499999999999999, 0.556492338826592, 0.612344665711926, 0.663693875664927, 0.706748335783171, 0.738036443737851, 0.754647908947033, 0.754452750202530, 0.736283812323626, 0.700070292479358, 0.646912255161750, 0.579089274357230, 0.500000000000002, 0.414033397437796, 0.326376425717934, 0.242766766812256, 0.169202662746925, 0.111624749690876, 0.0755868184216129, 0.0659335437721544, 0.0865033284336557, 0.139873473537157, 0.227162954699605, 0.347905241620710, 0.499999999999998, 0.679748350811884, 0.881971863420543, 1.10021087743807, 1.32699334313268, 1.55416139369619, 1.77323954473516, 1.97582595117467, 2.15398668626537, 2.30063263231421, 2.40985931710274, 2.47723185893074, 2.5, 2.47723185893074, 2.40985931710274, 2.30063263231421, 2.15398668626537, 1.97582595117467, 1.77323954473516, 1.55416139369619, 1.32699334313268, 1.10021087743807, 0.881971863420543, 0.679748350811884, 0.499999999999998, 0.347905241620710, 0.227162954699605, 0.139873473537157, 0.0865033284336557, 0.0659335437721544, 0.0755868184216129, 0.111624749690876, 0.169202662746925, 0.242766766812256, 0.326376425717934, 0.414033397437796, 0.500000000000002, 0.579089274357230, 0.646912255161750, 0.700070292479358, 0.736283812323626, 0.754452750202530, 0.754647908947033, 0.738036443737851, 0.706748335783171, 0.663693875664927, 0.612344665711926, 0.556492338826592, 0.499999999999999, 0.446561301109979, 0.399481088573539, 0.361489797514291, 0.334601331373462, 0.320021225466503, 0.318108636466405, 0.328392331258759, 0.349637573975875, 0.379957824512386, 0.416962638386838, 0.457931237044028, 0.500000000000000, 0.540351670590423, 0.576394372684110, 0.605919566606719, 0.627229745097337, 0.639228863318366, 0.641471060526129, 0.634165995561334, 0.618141906161812, 0.594770138542853, 0.565857217831129, 0.533512404388656, 0.500000000000000, 0.467586362968348, 0.438391634932169, 0.414255588937418, 0.396625832108414, 0.386474926832717, 0.384250950478622, 0.389863734986965, 0.402706665513801, 0.421711624681991, 0.445432590939922, 0.472151663958723, 0.500000000000000, 0.527085367930558, 0.551617819381155, 0.572025305292569, 0.587051930856073, 0.595832853972382, 0.597941503441166, 0.593406705770549, 0.582699334313269, 0.566690097493119, 0.546581934563482, 0.523822070589527, 0.500000000000000, 0.476738448718462, 0.455584667044122, 0.437909219575372, 0.424818786987937, 0.417088429709288, 0.415117363684323, 0.418910662023369, 0.428087535379766, 0.441915076376961, 0.459364695380793, 0.479187033063887, 0.500000000000000, 0.520383833597224, 0.538976720757199, 0.554564625221643, 0.566159467450615, 0.573060690652212, 0.574896443807951, 0.571642036464790, 0.563614872548668, 0.551446646637549, 0.536035081454769, 0.518478802419913, 0.500000000000000, 0.481860258174947, 0.465275285143587, 0.451334253180697, 0.440929046919094, 0.434697966762183, 0.432987392382360, 0.435833654296753, 0.442965976335677, 0.453829932504764, 0.467629503099953, 0.483384606227473, 0.500000000000000, 0.516340759164717, 0.531309169132832, 0.543917869080835, 0.553354409234367, 0.559033038046987, 0.560630454511198, 0.558103383904515, 0.551687083945793, 0.541875177495680, 0.529382451032350, 0.515093372968937, 0.500000000000000, 0.485133595045633, 0.471494637058168, 0.459985941504129, 0.451353332756901, 0.446137739008224, 0.444641758924558, 0.446912735569256, 0.452743237535275, 0.461688667397570, 0.473100572998553, 0.486173203783701, 0.500000000000000, 0.513636081785729, 0.526162456398668, 0.536747604741106, 0.544702342872037, 0.549524360777674, 0.550929581789407, 0.548868408979294, 0.543525965428036, 0.535306522202239, 0.524803367754581, 0.512756334573747, 0.500000000000000, 0.487406166503626, 0.475824565606294, 0.466025799390298, 0.458650332843366, 0.454166895926252, 0.452842979824624, 0.454729265301390, 0.459658861310601, 0.467261224867014, 0.476989646781895, 0.488160288269876, 0.500000000000000, 0.511699596798407, 0.522468933142385, 0.531590046180951, 0.538464806657334, 0.542653929224702, 0.543904811887420, 0.542166455747848, 0.537590606506031, 0.530519197157868, 0.521459093450593, 0.511045988038719, 0.500000000000000, 0.489076067077731, 0.479012534976893, 0.470481432257144, 0.464043767689883, 0.460112812130414, 0.458927756621446, 0.460539413070196, 0.464808793909247, 0.471418529645806, 0.479896217714708, 0.489648000738582, 0.500000000000000, 0.510244724657672, 0.519689271310338, 0.527702040497142, 0.533754830331946, 0.537457511451134, 0.538583016507126, 0.537081054049615, 0.533079733725308, 0.526875113915138, 0.518909498189136, 0.509740058418378, 0.500000000000000, 0.490354966541801, 0.481457676532983, 0.473903874893997, 0.468192563725666, 0.464693159062807, 0.463621727293281, 0.465027821062211, 0.468792704032729, 0.474638977009659, 0.482150847503713, 0.490803572749159, 0.500000000000000, 0.509111667552676, 0.517521645111034, 0.524666200442660, 0.530072485204825, 0.533389727402142, 0.534411879587437, 0.533090267963558, 0.529535476540453, 0.524008435097523, 0.516901409885865, 0.508710272506303, 0.500000000000000, 0.491365799742661, 0.483392527677368, 0.476615160619296, 0.471482988167838, 0.468329915210844 };
        private double[] arb_signal_ch2 = new double[468] { 0.467352832186278, 0.468329915210844, 0.471482988167838, 0.476615160619296, 0.483392527677368, 0.491365799742661, 0.500000000000000, 0.508710272506303, 0.516901409885865, 0.524008435097523, 0.529535476540453, 0.533090267963558, 0.534411879587437, 0.533389727402142, 0.530072485204825, 0.524666200442660, 0.517521645111034, 0.509111667552676, 0.500000000000000, 0.490803572749159, 0.482150847503713, 0.474638977009659, 0.468792704032729, 0.465027821062211, 0.463621727293281, 0.464693159062807, 0.468192563725666, 0.473903874893997, 0.481457676532983, 0.490354966541801, 0.500000000000000, 0.509740058418378, 0.518909498189136, 0.526875113915138, 0.533079733725308, 0.537081054049615, 0.538583016507126, 0.537457511451134, 0.533754830331946, 0.527702040497142, 0.519689271310338, 0.510244724657672, 0.500000000000000, 0.489648000738582, 0.479896217714708, 0.471418529645806, 0.464808793909247, 0.460539413070196, 0.458927756621446, 0.460112812130414, 0.464043767689883, 0.470481432257144, 0.479012534976893, 0.489076067077731, 0.500000000000000, 0.511045988038719, 0.521459093450593, 0.530519197157868, 0.537590606506031, 0.542166455747848, 0.543904811887420, 0.542653929224702, 0.538464806657334, 0.531590046180951, 0.522468933142385, 0.511699596798407, 0.500000000000000, 0.488160288269876, 0.476989646781895, 0.467261224867014, 0.459658861310601, 0.454729265301390, 0.452842979824624, 0.454166895926252, 0.458650332843366, 0.466025799390298, 0.475824565606294, 0.487406166503626, 0.500000000000000, 0.512756334573747, 0.524803367754581, 0.535306522202239, 0.543525965428036, 0.548868408979294, 0.550929581789407, 0.549524360777674, 0.544702342872037, 0.536747604741106, 0.526162456398668, 0.513636081785729, 0.500000000000000, 0.486173203783701, 0.473100572998553, 0.461688667397570, 0.452743237535275, 0.446912735569256, 0.444641758924558, 0.446137739008224, 0.451353332756901, 0.459985941504129, 0.471494637058168, 0.485133595045633, 0.500000000000000, 0.515093372968937, 0.529382451032350, 0.541875177495680, 0.551687083945793, 0.558103383904515, 0.560630454511198, 0.559033038046987, 0.553354409234367, 0.543917869080835, 0.531309169132832, 0.516340759164717, 0.500000000000000, 0.483384606227473, 0.467629503099953, 0.453829932504764, 0.442965976335677, 0.435833654296753, 0.432987392382360, 0.434697966762183, 0.440929046919094, 0.451334253180697, 0.465275285143587, 0.481860258174947, 0.500000000000000, 0.518478802419913, 0.536035081454769, 0.551446646637549, 0.563614872548668, 0.571642036464790, 0.574896443807951, 0.573060690652212, 0.566159467450615, 0.554564625221643, 0.538976720757199, 0.520383833597224, 0.500000000000000, 0.479187033063887, 0.459364695380793, 0.441915076376961, 0.428087535379766, 0.418910662023369, 0.415117363684323, 0.417088429709288, 0.424818786987937, 0.437909219575372, 0.455584667044122, 0.476738448718462, 0.500000000000000, 0.523822070589527, 0.546581934563482, 0.566690097493119, 0.582699334313269, 0.593406705770549, 0.597941503441166, 0.595832853972382, 0.587051930856073, 0.572025305292569, 0.551617819381155, 0.527085367930558, 0.500000000000000, 0.472151663958723, 0.445432590939922, 0.421711624681991, 0.402706665513801, 0.389863734986965, 0.384250950478622, 0.386474926832717, 0.396625832108414, 0.414255588937418, 0.438391634932169, 0.467586362968348, 0.500000000000000, 0.533512404388656, 0.565857217831129, 0.594770138542853, 0.618141906161812, 0.634165995561334, 0.641471060526129, 0.639228863318366, 0.627229745097337, 0.605919566606719, 0.576394372684110, 0.540351670590423, 0.500000000000000, 0.457931237044028, 0.416962638386838, 0.379957824512386, 0.349637573975875, 0.328392331258759, 0.318108636466405, 0.320021225466503, 0.334601331373462, 0.361489797514291, 0.399481088573539, 0.446561301109979, 0.499999999999999, 0.556492338826592, 0.612344665711926, 0.663693875664927, 0.706748335783171, 0.738036443737851, 0.754647908947033, 0.754452750202530, 0.736283812323626, 0.700070292479358, 0.646912255161750, 0.579089274357230, 0.500000000000002, 0.414033397437796, 0.326376425717934, 0.242766766812256, 0.169202662746925, 0.111624749690876, 0.0755868184216129, 0.0659335437721544, 0.0865033284336557, 0.139873473537157, 0.227162954699605, 0.347905241620710, 0.499999999999998, 0.679748350811884, 0.881971863420543, 1.10021087743807, 1.32699334313268, 1.55416139369619, 1.77323954473516, 1.97582595117467, 2.15398668626537, 2.30063263231421, 2.40985931710274, 2.47723185893074, 2.5, 2.47723185893074, 2.40985931710274, 2.30063263231421, 2.15398668626537, 1.97582595117467, 1.77323954473516, 1.55416139369619, 1.32699334313268, 1.10021087743807, 0.881971863420543, 0.679748350811884, 0.499999999999998, 0.347905241620710, 0.227162954699605, 0.139873473537157, 0.0865033284336557, 0.0659335437721544, 0.0755868184216129, 0.111624749690876, 0.169202662746925, 0.242766766812256, 0.326376425717934, 0.414033397437796, 0.500000000000002, 0.579089274357230, 0.646912255161750, 0.700070292479358, 0.736283812323626, 0.754452750202530, 0.754647908947033, 0.738036443737851, 0.706748335783171, 0.663693875664927, 0.612344665711926, 0.556492338826592, 0.499999999999999, 0.446561301109979, 0.399481088573539, 0.361489797514291, 0.334601331373462, 0.320021225466503, 0.318108636466405, 0.328392331258759, 0.349637573975875, 0.379957824512386, 0.416962638386838, 0.457931237044028, 0.500000000000000, 0.540351670590423, 0.576394372684110, 0.605919566606719, 0.627229745097337, 0.639228863318366, 0.641471060526129, 0.634165995561334, 0.618141906161812, 0.594770138542853, 0.565857217831129, 0.533512404388656, 0.500000000000000, 0.467586362968348, 0.438391634932169, 0.414255588937418, 0.396625832108414, 0.386474926832717, 0.384250950478622, 0.389863734986965, 0.402706665513801, 0.421711624681991, 0.445432590939922, 0.472151663958723, 0.500000000000000, 0.527085367930558, 0.551617819381155, 0.572025305292569, 0.587051930856073, 0.595832853972382, 0.597941503441166, 0.593406705770549, 0.582699334313269, 0.566690097493119, 0.546581934563482, 0.523822070589527, 0.500000000000000, 0.476738448718462, 0.455584667044122, 0.437909219575372, 0.424818786987937, 0.417088429709288, 0.415117363684323, 0.418910662023369, 0.428087535379766, 0.441915076376961, 0.459364695380793, 0.479187033063887, 0.500000000000000, 0.520383833597224, 0.538976720757199, 0.554564625221643, 0.566159467450615, 0.573060690652212, 0.574896443807951, 0.571642036464790, 0.563614872548668, 0.551446646637549, 0.536035081454769, 0.518478802419913, 0.500000000000000, 0.481860258174947, 0.465275285143587, 0.451334253180697, 0.440929046919094, 0.434697966762183, 0.432987392382360, 0.435833654296753, 0.442965976335677, 0.453829932504764, 0.467629503099953, 0.483384606227473, 0.500000000000000, 0.516340759164717, 0.531309169132832, 0.543917869080835, 0.553354409234367, 0.559033038046987, 0.560630454511198, 0.558103383904515, 0.551687083945793, 0.541875177495680, 0.529382451032350, 0.515093372968937, 0.500000000000000, 0.485133595045633, 0.471494637058168, 0.459985941504129, 0.451353332756901, 0.446137739008224, 0.444641758924558, 0.446912735569256, 0.452743237535275, 0.461688667397570, 0.473100572998553, 0.486173203783701, 0.500000000000000, 0.513636081785729, 0.526162456398668, 0.536747604741106, 0.544702342872037, 0.549524360777674, 0.550929581789407, 0.548868408979294, 0.543525965428036, 0.535306522202239, 0.524803367754581, 0.512756334573747, 0.500000000000000, 0.487406166503626, 0.475824565606294, 0.466025799390298, 0.458650332843366, 0.454166895926252, 0.452842979824624, 0.454729265301390, 0.459658861310601, 0.467261224867014, 0.476989646781895, 0.488160288269876, 0.500000000000000, 0.511699596798407, 0.522468933142385, 0.531590046180951, 0.538464806657334, 0.542653929224702, 0.543904811887420, 0.542166455747848, 0.537590606506031, 0.530519197157868, 0.521459093450593, 0.511045988038719, 0.500000000000000, 0.489076067077731, 0.479012534976893, 0.470481432257144, 0.464043767689883, 0.460112812130414, 0.458927756621446, 0.460539413070196, 0.464808793909247, 0.471418529645806, 0.479896217714708, 0.489648000738582, 0.500000000000000, 0.510244724657672, 0.519689271310338, 0.527702040497142, 0.533754830331946, 0.537457511451134, 0.538583016507126, 0.537081054049615, 0.533079733725308, 0.526875113915138, 0.518909498189136, 0.509740058418378, 0.500000000000000, 0.490354966541801, 0.481457676532983, 0.473903874893997, 0.468192563725666, 0.464693159062807, 0.463621727293281, 0.465027821062211, 0.468792704032729, 0.474638977009659, 0.482150847503713, 0.490803572749159, 0.500000000000000, 0.509111667552676, 0.517521645111034, 0.524666200442660, 0.530072485204825, 0.533389727402142, 0.534411879587437, 0.533090267963558, 0.529535476540453, 0.524008435097523, 0.516901409885865, 0.508710272506303, 0.500000000000000, 0.491365799742661, 0.483392527677368, 0.476615160619296, 0.471482988167838, 0.468329915210844 };
 
        private double[] time_ch1;
        private double[] time_ch2;

        private int actual_channels = 1;

        public GraphPane channel1Pane;
        public GraphPane channel2Pane;

        private Queue<Message> gen_q = new Queue<Message>();
        Message messg;

        const int DATA_BLOCK = 32;
        int toSend = 0;
        int sent = 0;
        int index = 0;
        int actualSend = 0;
        private bool generating = false;
        int sendingChannel;

        double realFreq_ch1=0;
        double realFreq_ch2 = 0;

        public Generator(Device dev)
        {
            InitializeComponent();
            zedGraphControl_gen_ch1.MasterPane[0].IsFontsScaled = false;
            zedGraphControl_gen_ch1.MasterPane[0].Title.IsVisible = false;
            zedGraphControl_gen_ch1.MasterPane[0].XAxis.MajorGrid.IsVisible = true;
            zedGraphControl_gen_ch1.MasterPane[0].XAxis.Title.IsVisible = false;
            zedGraphControl_gen_ch1.MasterPane[0].XAxis.IsVisible = false;

            zedGraphControl_gen_ch1.MasterPane[0].YAxis.MajorGrid.IsVisible = true;
            zedGraphControl_gen_ch1.MasterPane[0].YAxis.Title.IsVisible = false;

            zedGraphControl_gen_ch2.MasterPane[0].IsFontsScaled = false;
            zedGraphControl_gen_ch2.MasterPane[0].Title.IsVisible = false;
            zedGraphControl_gen_ch2.MasterPane[0].XAxis.MajorGrid.IsVisible = true;
            zedGraphControl_gen_ch2.MasterPane[0].XAxis.Title.IsVisible = false;
            zedGraphControl_gen_ch2.MasterPane[0].XAxis.IsVisible = false;

            zedGraphControl_gen_ch2.MasterPane[0].YAxis.MajorGrid.IsVisible = true;
            zedGraphControl_gen_ch2.MasterPane[0].YAxis.Title.IsVisible = false;

            channel1Pane = zedGraphControl_gen_ch1.GraphPane;
            channel2Pane = zedGraphControl_gen_ch2.GraphPane;

            this.device = dev;
            this.trackBar_ampl_ch1.Maximum = dev.genCfg.VRef;
            this.trackBar_ampl_ch2.Maximum = dev.genCfg.VRef;
            this.trackBar_ampl_ch1.Value = dev.genCfg.VRef / 2;
            this.trackBar_ampl_ch2.Value = dev.genCfg.VRef / 2;
            this.textBox_ampl_ch1.Text = (dev.genCfg.VRef / 2).ToString();
            this.textBox_ampl_ch2.Text = (dev.genCfg.VRef / 2).ToString();

            this.trackBar_offset_ch1.Maximum = dev.genCfg.VRef;
            this.trackBar_offset_ch2.Maximum = dev.genCfg.VRef;
            this.trackBar_offset_ch1.Value = dev.genCfg.VRef / 2;
            this.trackBar_offset_ch2.Value = dev.genCfg.VRef / 2;
            this.trackBar_offset_ch1.Text = (dev.genCfg.VRef / 2).ToString();
            this.trackBar_offset_ch2.Text = (dev.genCfg.VRef / 2).ToString();

            this.trackBar_phase_ch2.Value = 900;

            freq_ch1 = trackBar_freq_ch1.Value/10;
            freq_ch2 = trackBar_freq_ch2.Value/10;

            signal_leng = int.Parse(this.toolStripTextBox_signal_leng.Text);

            validate_control_ch2();

            signalTimer = new System.Timers.Timer(200);
            signalTimer.Elapsed += new ElapsedEventHandler(Update_signal);
            signalTimer.Start();
            
            dataSendingTimer = new System.Timers.Timer(5);
            dataSendingTimer.Elapsed += new ElapsedEventHandler(data_sending);
            
            
        }

        private void data_sending(object sender, ElapsedEventArgs e)
        {
            if (gen_q.Count > 0)
            {
                messg = gen_q.Dequeue();
                if (messg == null)
                {
                    return;
                }
                switch (messg.GetRequest())
                {
                    case Message.MsgRequest.GEN_NEXT:
                        
                        if (toSend == 0)
                        {
                            if (sendingChannel == 1 && actual_channels == 2) {
                                toSend = signal_ch2.Length;
                                sent = 0;
                                index = 0;
                                actualSend = 0;
                                sendingChannel = 2;
                                send_next(signal_ch2, 2);
                            }
                            else if (sendingChannel == actual_channels) {
                                gen_get_freq();
                                Thread.Sleep(10);
                                gen_start();
                            }
                        }
                        else {
                            if (sendingChannel == 2)
                            {
                                send_next(signal_ch2, 2);
                            }
                            else {
                                send_next(signal_ch1, 1);
                            }
                        }
                        break;
                    case Message.MsgRequest.GEN_OK:
                        generating = true;
                        this.Invalidate();
                        break;
                    case Message.MsgRequest.GEN_FRQ:
                        if(messg.GetMessage().Equals(Commands.CHANNELS_1)){
                            this.realFreq_ch1 = (double)messg.GetNum() / signal_leng_ch1;
                        }
                        else if (messg.GetMessage().Equals(Commands.CHANNELS_2)) {
                            this.realFreq_ch2 = (double)messg.GetNum() / signal_leng_ch2;
                        }
                        this.Invalidate();
                        break;

                        
                }
            }
        }

        public void add_message(Message msg)
        {
            this.gen_q.Enqueue(msg);
        }

        private void Update_signal(object sender, ElapsedEventArgs e)
        {
            double sum = signalType_ch1.GetHashCode() + signalType_ch2.GetHashCode() + freq_ch1 + freq_ch2 + ampl_ch1 + ampl_ch2 + phase_ch1 + phase_ch2 + duty_ch1 + duty_ch2 + offset_ch1 + offset_ch2 + signal_leng_ch1 + signal_leng_ch2 + signal_leng + actual_channels;
            sum = bestFreqFit ? sum+1 : sum;
            sum = customLeng ? sum+2 : sum;
            sum = khz_ch1 ? sum+4 : sum;
            sum = khz_ch2 ? sum+8 : sum;
            sum = generating ? sum + 16 : sum;
            sum = frequencyJoin ? sum + 32 : sum;

            if (sum != last_sum) {
                last_sum = sum;

                if (!generating)
                {
                    takeGenSemaphore(4852);
                    calculate_signal_lengths();
                    generate_signals();
                    paint_signals();
                    giveGenSemaphore();
                }
                else {
                    double tmpFreq = freq_ch1*signal_leng_ch1;
                    if (frequencyJoin) {
                        gen_stop();
                    }
                    tmpFreq = khz_ch1 ? tmpFreq * 1000 : tmpFreq;
                    set_frequency((int)tmpFreq, 1);

                    if (actual_channels == 2) {
                        tmpFreq = freq_ch2 * signal_leng_ch2;
                        tmpFreq = khz_ch2 ? tmpFreq * 1000 : tmpFreq;
                        set_frequency((int)tmpFreq, 2);
                    }
                    if (frequencyJoin) {
                        gen_start();
                    }

                    gen_get_freq();
                }
                this.Invalidate();
            }
        }

        private void paint_signals() {
            //plot signal
            channel1Pane.CurveList.Clear();
            LineItem curve;
            curve = channel1Pane.AddCurve("", time_ch1, signal_ch1, Color.Red, SymbolType.Diamond);
            curve.Line.IsSmooth = false;
            curve.Line.SmoothTension = 0.5F;
            curve.Line.IsAntiAlias = false;
            curve.Line.IsOptimizedDraw = true;
            curve.Symbol.Size = 0;

            channel1Pane.XAxis.Scale.MaxAuto = false;
            channel1Pane.XAxis.Scale.MinAuto = false;
            channel1Pane.YAxis.Scale.MaxAuto = false;
            channel1Pane.YAxis.Scale.MinAuto = false;

            channel1Pane.XAxis.Scale.Max = time_ch1[time_ch1.Length - 1]+time_ch1[1];
            channel1Pane.XAxis.Scale.Min = 0;
            channel1Pane.YAxis.Scale.Max = (double)(device.genCfg.VRef) / 1000;
            channel1Pane.YAxis.Scale.Min = 0;

            if (actual_channels == 2)
            {
                channel2Pane.CurveList.Clear();
                curve = channel2Pane.AddCurve("", time_ch2, signal_ch2, Color.Blue, SymbolType.Diamond);
                curve.Line.IsSmooth = false;
                curve.Line.SmoothTension = 0.5F;
                curve.Line.IsAntiAlias = false;
                curve.Line.IsOptimizedDraw = true;
                curve.Symbol.Size = 0;

                channel2Pane.XAxis.Scale.MaxAuto = false;
                channel2Pane.XAxis.Scale.MinAuto = false;
                channel2Pane.YAxis.Scale.MaxAuto = false;
                channel2Pane.YAxis.Scale.MinAuto = false;

                channel2Pane.XAxis.Scale.Max = time_ch2[time_ch2.Length - 1] + time_ch2[1];
                channel2Pane.XAxis.Scale.Min = 0;
                channel2Pane.YAxis.Scale.Max = (double)(device.genCfg.VRef) / 1000;
                channel2Pane.YAxis.Scale.Min = 0;
            }
        }

        public void generate_signals() {
            //generate signals
            for (int i = 1; i <= actual_channels; i++)
            {
                SIGNAL_TYPE tmpSigType;
                double[] tmpSignal;
                double[] tmpSrcSignal;
                double[] tmpTime;
                double tmpAmpl;
                double tmpPhase;
                double tmpDuty;
                double tmpOffset;
                int tmpDiv;
                int shift;
                if (i == 1)
                {
                    tmpSignal = new double[signal_leng_ch1];
                    tmpSrcSignal = arb_signal_ch1;
                    tmpSigType = signalType_ch1;
                    tmpAmpl = ampl_ch1;
                    tmpPhase = phase_ch1;
                    tmpDuty = duty_ch1;
                    tmpOffset = offset_ch1;
                    tmpDiv = divider_ch1;
                    
                }
                else
                {
                    tmpSignal = new double[signal_leng_ch2];
                    tmpSrcSignal = arb_signal_ch2;
                    tmpSigType = signalType_ch2;
                    tmpAmpl = ampl_ch2;
                    tmpPhase = phase_ch2;
                    tmpDuty = duty_ch2;
                    tmpOffset = offset_ch2;
                    tmpDiv = divider_ch2;
                }

                tmpTime = new double[tmpSignal.Length];
                for (int j = 0; j < tmpSignal.Length; j++)
                {
                    tmpTime[j] = (double)j * tmpDiv / device.systemCfg.PeriphClock;
                }

                switch (tmpSigType)
                {
                    case SIGNAL_TYPE.SINE:
                        for (int j = 0; j < tmpSignal.Length; j++)
                        {
                            tmpSignal[j] = (tmpAmpl / 1000 * Math.Sin(2 * Math.PI * j / tmpSignal.Length + tmpPhase * Math.PI / 180) + tmpOffset / 1000);
                        }
                        break;
                    case SIGNAL_TYPE.SQUARE:
                        shift = (int)(tmpPhase / 360 * tmpSignal.Length);
                        for (int j = 0; j < tmpSignal.Length; j++)
                        {
                            if (j < tmpDuty / 100 * tmpSignal.Length)
                            {
                                tmpSignal[(j + shift) % tmpSignal.Length] = tmpOffset / 1000 - tmpAmpl / 1000;
                            }
                            else
                            {
                                tmpSignal[(j + shift) % tmpSignal.Length] = tmpOffset / 1000 + tmpAmpl / 1000;
                            }
                        }
                        break;
                    case SIGNAL_TYPE.SAW:
                        shift = (int)(tmpPhase / 360 * tmpSignal.Length);
                        for (int j = 0; j < tmpSignal.Length; j++)
                        {
                            if (j > tmpSignal.Length * tmpDuty / 100)
                            {
                                tmpSignal[(j + shift) % tmpSignal.Length] = (tmpOffset - tmpAmpl + tmpAmpl * 2 - tmpAmpl * 2 / (tmpSignal.Length - (tmpDuty / 100 * tmpSignal.Length)) * (j - (tmpSignal.Length * tmpDuty / 100))) / 1000;
                            }
                            else
                            {
                                tmpSignal[(j + shift) % tmpSignal.Length] = (tmpOffset - tmpAmpl + tmpAmpl * 2 / (tmpDuty / 100 * tmpSignal.Length) * j) / 1000;
                            }
                        }
                        break;
                    case SIGNAL_TYPE.ARB:
                        shift = (int)(tmpPhase / 360 * tmpSignal.Length);
                        tmpSignal = new double[tmpSrcSignal.Length];
                        for (int j = 0; j < tmpSignal.Length; j++)
                        {
                            tmpSignal[(j + shift) % tmpSignal.Length]=tmpSrcSignal[j]*tmpAmpl/1500+(tmpOffset-1500)/1000;
                        }
                        break;

                }

                if (i == 1)
                {
                    signal_ch1 = tmpSignal;
                    time_ch1 = tmpTime;
                }
                else
                {
                    signal_ch2 = tmpSignal;
                    time_ch2 = tmpTime;
                }
            }
        }

        public void calculate_signal_lengths() {
            int tclk = device.systemCfg.PeriphClock;
            //estimate length and divider
            /*
             * *Best frequency fit*
             * 1 - Estimate minimal possible divider (sampling frequency or max length of signal)
             * 2 - Calculate desired signal length for current divider
             * 3 - Round signal length and calculate error 
             * 4 - Increment divider and calculate error again while error small enough or signal length too small
             */
            if (bestFreqFit)
            {
                
                double tmp_freq = checkBox_khz_ch1.Checked ? freq_ch1 * 1000 : freq_ch1;

                int divA = tclk / device.genCfg.maxSamplingFrequency;
                int divB = (int)(tclk / tmp_freq / (device.genCfg.BufferLength/2) * actual_channels);
                int div = divA > divB ? divA : divB;
                double error;
                double minimalError;
                int bestDiv = 0;
                int bestLeng = 0;
                double tmpSigLeng = 0; ;

                for (int i = 1; i <= actual_channels; i++)
                {
                    if ((i == 1 && signalType_ch1 == SIGNAL_TYPE.ARB) || (i == 2 && signalType_ch2 == SIGNAL_TYPE.ARB))
                    {
                        divider_ch1 = (int)(tclk / freq_ch1 / signal_leng_ch1);
                        divider_ch2= (int)(tclk / freq_ch2 / signal_leng_ch2);
                        continue;
                    }

                    error = double.MaxValue;
                    minimalError = double.MaxValue;
                    if (i == 2)
                    {
                        tmp_freq = checkBox_khz_ch2.Checked ? freq_ch2 * 1000 : freq_ch2;
                        divB = (int)Math.Round((double)tclk / tmp_freq / (device.genCfg.BufferLength / 2) * actual_channels);
                        div = divA > divB ? divA : divB;
                    }

                    int iter = 0;
                    while (error > 0)
                    {
                        tmpSigLeng = tclk / tmp_freq / div;
                        error = Math.Abs(tmp_freq - (double)(tclk) / (div * (int)Math.Round(tmpSigLeng)));

                        if (tmpSigLeng-0.0001 > (device.genCfg.BufferLength / 2 / actual_channels))
                        {
                            div++;
                            iter++;
                            continue;
                        }
                        if (error < minimalError)
                        {
                            bestDiv = div;
                            bestLeng = (int)Math.Round(tmpSigLeng);
                            minimalError = error;
                        }

                        if (error < 0.01)
                        {
                            break;
                        }

                        if (tmpSigLeng < (device.genCfg.BufferLength / 2 / actual_channels / 4) && iter > 5)
                        {
                            break;
                        }
                        div++;
                        iter++;
                    }

                    if (i == 1)
                    {
                        signal_leng_ch1 = bestLeng;
                        divider_ch1 = bestDiv;
                    }
                    else
                    {
                        signal_leng_ch2 = bestLeng;
                        divider_ch2 = bestDiv;
                    }
                }
            }
            else
            { //isn't best frequency fit 
                double tmp_freq_ch1 = checkBox_khz_ch1.Checked ? freq_ch1 * 1000 : freq_ch1;
                double tmp_freq_ch2 = checkBox_khz_ch2.Checked ? freq_ch2 * 1000 : freq_ch2;
                if (customLeng)  //custom length
                {
                    if (signalType_ch1 != SIGNAL_TYPE.ARB)
                    {
                        //int.TryParse(toolStripTextBox_signal_leng.Text, out signal_leng_ch1);
                        signal_leng_ch1 = signal_leng;
                    }

                    if (signalType_ch2 != SIGNAL_TYPE.ARB)
                    {
                        //int.TryParse(toolStripTextBox_signal_leng.Text, out signal_leng_ch2);
                        signal_leng_ch2 = signal_leng;
                    }

                    int divA = tclk / device.genCfg.maxSamplingFrequency;
                    int divB = (int)(tclk / tmp_freq_ch1 / signal_leng_ch1);
                    divider_ch1 = divA > divB ? divA : divB;

                    divA = tclk / device.genCfg.maxSamplingFrequency;
                    divB = (int)(tclk / tmp_freq_ch2 / signal_leng_ch2);
                    divider_ch2 = divA > divB ? divA : divB;
                }
                else //maximum possible
                {
                    int div = tclk / device.genCfg.maxSamplingFrequency;
                    double leng = (double)tclk / tmp_freq_ch1 / div;
                    if (signalType_ch1 != SIGNAL_TYPE.ARB)
                    {
                        if (leng > device.genCfg.BufferLength / 2 / actual_channels)
                        {
                            signal_leng_ch1 = device.genCfg.BufferLength / 2 / actual_channels;
                            divider_ch1 = (int)(tclk / tmp_freq_ch1 / signal_leng_ch1);
                        }
                        else
                        {
                            signal_leng_ch1 = (int)leng;
                            divider_ch1 = (int)(tclk / tmp_freq_ch1 / signal_leng_ch1);
                        }
                    }
                    else {
                        divider_ch1 = (int)(tclk / freq_ch1 / signal_leng_ch1);
                    }

                    leng = (double)tclk / tmp_freq_ch2 / div;
                    if (signalType_ch2 != SIGNAL_TYPE.ARB)
                    {
                        if (leng > device.genCfg.BufferLength / 2 / actual_channels)
                        {
                            signal_leng_ch2 = device.genCfg.BufferLength / 2 / actual_channels;
                            divider_ch2 = (int)(tclk / tmp_freq_ch2 / signal_leng_ch2);
                        }
                        else
                        {
                            signal_leng_ch2 = (int)leng;
                            divider_ch2 = (int)(tclk / tmp_freq_ch2 / signal_leng_ch2);
                        }
                    }
                    else
                    {
                        divider_ch2 = (int)(tclk / freq_ch2 / signal_leng_ch2);
                    }
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            if (this.generating) {
                button_gen_control.Enabled = true;
                label_status.BackColor = Color.LightGreen;
                label_status_gen.Text = "Generating";
                this.button_gen_control.Enabled=true;
                this.button_gen_control.Text = "Disable";
            }
            zedGraphControl_gen_ch1.Refresh();
            zedGraphControl_gen_ch2.Refresh();
            if (generating)
            {
                label_real_freq_ch1_title.Text = "Real frequency";
                label_real_freq_ch1.Text = Math.Round((double)realFreq_ch1, 2).ToString()+ " Hz";
                if (actual_channels == 2)
                {
                    label_real_freq_ch2.Text = Math.Round((double)realFreq_ch2, 2).ToString()+" Hz";
                }
            }
            else
            {
                label_real_freq_ch1_title.Text = "Estimate freq.";
                label_real_freq_ch1.Text = Math.Round((double)(device.systemCfg.PeriphClock) / signal_leng_ch1 / divider_ch1, 2).ToString()+" Hz";
                label_sig_leng_ch1.Text = signal_leng_ch1.ToString();// +" " + divider_ch1.ToString();
                if (actual_channels == 2)
                {
                    label_sig_leng_ch2.Text = signal_leng_ch2.ToString();// +" " + divider_ch2.ToString();
                    label_real_freq_ch2.Text = Math.Round((double)(device.systemCfg.PeriphClock) / signal_leng_ch2 / divider_ch2, 2).ToString()+ "Hz";
                }
            }
            

            
            base.OnPaint(e);
        }

        //communication with device
        public void gen_start()
        {
            device.takeCommsSemaphore(semaphoreTimeout+101);
            device.send(Commands.GENERATOR + ":" + Commands.START + ";");
            device.giveCommsSemaphore();
        }

        public void gen_stop()
        {
            device.takeCommsSemaphore(semaphoreTimeout+102);
            device.send(Commands.GENERATOR + ":" + Commands.STOP + ";");
            device.giveCommsSemaphore();
        }
        
        public void set_data_length(string chan,int len) {
            device.takeCommsSemaphore(semaphoreTimeout + 103);
            device.send(Commands.GENERATOR + ":" + chan + " ");
            device.send_short((int)(len));
            device.send(";");
            device.giveCommsSemaphore();
        }

        public void set_num_of_channels(string chann)
        {
            device.takeCommsSemaphore(semaphoreTimeout + 104);
            device.send(Commands.GENERATOR + ":" + Commands.CHANNELS + " " + chann + ";");
            device.giveCommsSemaphore();
        }

        public void set_frequency(int freq, int chann) {
            device.takeCommsSemaphore(semaphoreTimeout + 105);
            device.send(Commands.GENERATOR + ":" + Commands.SAMPLING_FREQ + " ");
            device.send_int(freq*256 + chann);
            device.send(";");
            device.giveCommsSemaphore();
        }

        public void gen_get_freq() {
            device.takeCommsSemaphore(semaphoreTimeout + 106);
            device.send(Commands.GENERATOR + ":" + Commands.GET_REAL_SMP_FREQ + ";");
            device.giveCommsSemaphore();
        }

        public void gen_set_out_buff()
        {
            device.takeCommsSemaphore(semaphoreTimeout + 106);
            device.send(Commands.GENERATOR + ":" + Commands.GEN_BUFF_ON + ";");
            device.giveCommsSemaphore();
        }


        public void gen_unset_out_buff()
        {
            device.takeCommsSemaphore(semaphoreTimeout + 106);
            device.send(Commands.GENERATOR + ":" + Commands.GEN_BUFF_OFF + ";");
            device.giveCommsSemaphore();
        }

        public void send_next(double[] data, int chann)
        {
            device.takeCommsSemaphore(semaphoreTimeout+107);
            int tmpData;
            device.send(Commands.GENERATOR + ":" + Commands.GEN_DATA + " ");
            if (toSend > DATA_BLOCK)
            {
                actualSend = DATA_BLOCK;
            }
            else
            {
                actualSend = toSend;
            }
            device.send_int((index / 256) + (index % 256) * 256 + (actualSend * 256 * 256) + (chann * 256 * 256 * 256));
            device.send(":");
            for (int i = 0; i < actualSend; i++)
            {
                tmpData = (int)Math.Round(data[sent + i] / device.genCfg.VRef * 1000 * (Math.Pow(2, device.genCfg.dataDepth) - 1));
                if (tmpData > Math.Pow(2, device.genCfg.dataDepth) - 1) {
                    tmpData = (int)Math.Pow(2, device.genCfg.dataDepth) - 1;
                }
                else if (tmpData < 0) {
                    tmpData = 0;
                }
                device.send_short_2byte(tmpData);
            }
            sent += actualSend;
            toSend -= actualSend;
            index += actualSend;
            device.send(";");
            device.giveCommsSemaphore();
        }


        // END communication with device

       

        private void Generator_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (generating) {
                gen_stop();
            }
        }


        private void maximumPossibleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            customLeng = false;
            bestFreqFit = false;
            validateFreqFit();
        }

        private void bestFreqFitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            customLeng = false;
            bestFreqFit = true;
            validateFreqFit();
        }

        private void customToolStripMenuItem_Click(object sender, EventArgs e)
        {
            customLeng = true;
            bestFreqFit = false;
            validateFreqFit();
        }

        private void validateFreqFit() {
            if (!customLeng)
            {
                maximumPossibleToolStripMenuItem.Checked = !bestFreqFit ? true : false;
                bestFreqFitToolStripMenuItem.Checked = bestFreqFit ? true : false;
                customToolStripMenuItem.Checked = false;
            }
            else {
                maximumPossibleToolStripMenuItem.Checked =  false;
                bestFreqFitToolStripMenuItem.Checked = false;
                customToolStripMenuItem.Checked = true;
            }

            //if (!bestFreqFit && !customLeng)
            //{
            //    signal_leng_ch1 = device.genCfg.BufferLength / 2 / actual_channels;
            //    signal_leng_ch2 = device.genCfg.BufferLength / 2 / actual_channels;
            //}
        }


        // track-bar and tex-box functions

        private void trackBar_freq_ch1_ValueChanged(object sender, EventArgs e)
        {
            if (this.trackBar_freq_ch1.Value < 0.1)
            {
                freq_ch1 = 0.1;
            }
            else
            {
                freq_ch1 = ((double)(this.trackBar_freq_ch1.Value) / 10);
            }
            if (frequencyJoin) {
                this.trackBar_freq_ch2.Value = this.trackBar_freq_ch1.Value;
            }
            this.textBox_freq_ch1.Text = freq_ch1.ToString();
        }

        private void textBox_freq_ch1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_freq_ch1();
            }
        }

        private void textBox_freq_ch1_Leave(object sender, EventArgs e)
        {
            validate_text_freq_ch1();
        }

        private void validate_text_freq_ch1()
        {
            try
            {
                Double val = Double.Parse(this.textBox_freq_ch1.Text);
                if (val < 1)
                {
                    if (khz_ch1)
                    {
                        khz_ch1 = false;
                        checkBox_khz_ch1.Checked = false;
                        val = val * 1000;
                    }
                    else
                    {
                        throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                    }
                }
                else if (val > 1000)
                {
                    if (!khz_ch1)
                    {
                        khz_ch1 = true;
                        checkBox_khz_ch1.Checked = true;
                        val = val / 1000;
                    }
                    else
                    {
                        throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                    }
                }
                this.trackBar_freq_ch1.Value = (int)(val * 10);
                freq_ch1 = val;
                
            }
            catch (Exception ex)
            {
            }
            finally { 
                this.textBox_freq_ch1.Text = freq_ch1.ToString();
            }
        }

        private void checkBox_khz_ch1_CheckedChanged(object sender, EventArgs e)
        {
            khz_ch1 = checkBox_khz_ch1.Checked;
        }

        private void textBox_freq_ch2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_freq_ch2();
            }
        }

        private void textBox_freq_ch2_Leave(object sender, EventArgs e)
        {
            validate_text_freq_ch2();
        }

        private void trackBar_freq_ch2_ValueChanged(object sender, EventArgs e)
        {
            if (this.trackBar_freq_ch2.Value < 0.1)
            {
                freq_ch2 = 0.1;
            }
            else
            {
                freq_ch2 = ((double)(this.trackBar_freq_ch2.Value) / 10);
            }
            if (frequencyJoin)
            {
                this.trackBar_freq_ch1.Value = this.trackBar_freq_ch2.Value;
            }
            this.textBox_freq_ch2.Text = freq_ch2.ToString();
        }

        private void checkBox_khz_ch2_CheckedChanged(object sender, EventArgs e)
        {
            khz_ch2 = checkBox_khz_ch2.Checked;
        }

        private void validate_text_freq_ch2()
        {
            try
            {
                Double val = Double.Parse(this.textBox_freq_ch2.Text);
                if (val < 1)
                {
                    if (khz_ch1)
                    {
                        khz_ch2 = false;
                        checkBox_khz_ch2.Checked = false;
                        val = val * 1000;
                    }
                    else
                    {
                        throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                    }
                }
                else if (val > 1000)
                {
                    if (!khz_ch2)
                    {
                        khz_ch2 = true;
                        checkBox_khz_ch2.Checked = true;
                        val = val / 1000;
                    }
                    else
                    {
                        throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                    }
                }
                this.trackBar_freq_ch2.Value = (int)(val * 10);
                freq_ch2 = val;

            }
            catch (Exception ex)
            {
            }
            finally
            {
                this.textBox_freq_ch2.Text = freq_ch2.ToString();
            }
        }



        private void trackBar_ampl_ch1_ValueChanged(object sender, EventArgs e)
        {
            ampl_ch1 = ((double)(this.trackBar_ampl_ch1.Value));
            this.textBox_ampl_ch1.Text = ampl_ch1.ToString();
        }

        private void textBox_ampl_ch1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_ampl_ch1();
            }
        }

        private void textBox_ampl_ch1_Leave(object sender, EventArgs e)
        {
            validate_text_ampl_ch1();
        }

        private void validate_text_ampl_ch1()
        {
            try
            {
                Double val = Double.Parse(this.textBox_ampl_ch1.Text);
                if (val > device.genCfg.VRef)
                {
                    throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                }
                this.trackBar_ampl_ch1.Value = (int)(val);
                ampl_ch1 = val;
            }
            catch (Exception ex)
            {
            }
            finally
            {
                this.textBox_ampl_ch1.Text = ampl_ch1.ToString();
            }
        }



        private void trackBar_ampl_ch2_ValueChanged(object sender, EventArgs e)
        {
            ampl_ch2 = ((double)(this.trackBar_ampl_ch2.Value));
            this.textBox_ampl_ch2.Text = ampl_ch2.ToString();
        }

        private void textBox_ampl_ch2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_ampl_ch2();
            }
        }

        private void textBox_ampl_ch2_Leave(object sender, EventArgs e)
        {
            validate_text_ampl_ch2();
        }

        private void validate_text_ampl_ch2()
        {
            try
            {
                Double val = Double.Parse(this.textBox_ampl_ch2.Text);
                if (val > device.genCfg.VRef)
                {
                    throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                }
                this.trackBar_ampl_ch2.Value = (int)(val);
                ampl_ch2 = val;
            }
            catch (Exception ex)
            {
            }
            finally
            {
                this.textBox_ampl_ch2.Text = ampl_ch2.ToString();
            }
        }


        private void trackBar_phase_ch1_ValueChanged(object sender, EventArgs e)
        {
            phase_ch1 = ((double)(this.trackBar_phase_ch1.Value) / 10);
            this.textBox_phase_ch1.Text = phase_ch1.ToString();
        }

        private void textBox_phase_ch1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_phase_ch1();
            }
        }

        private void textBox_phase_ch1_Leave(object sender, EventArgs e)
        {
            validate_text_phase_ch1();
        }

        private void validate_text_phase_ch1()
        {
            try
            {
                Double val = Double.Parse(this.textBox_phase_ch1.Text);
                if (val > 360)
                {
                    throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                }
                this.trackBar_phase_ch1.Value = (int)(val * 10);
                phase_ch1 = val;

            }
            catch (Exception ex)
            {
            }
            finally
            {
                this.textBox_phase_ch1.Text = phase_ch1.ToString();
            }
        }


        private void trackBar_phase_ch2_ValueChanged(object sender, EventArgs e)
        {
            phase_ch2 = ((double)(this.trackBar_phase_ch2.Value) / 10);
            this.textBox_phase_ch2.Text = phase_ch2.ToString();
        }

        private void textBox_phase_ch2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_phase_ch2();
            }
        }

        private void textBox_phase_ch2_Leave(object sender, EventArgs e)
        {
            validate_text_phase_ch2();
        }

        private void validate_text_phase_ch2()
        {
            try
            {
                Double val = Double.Parse(this.textBox_phase_ch2.Text);
                if (val > 360)
                {
                    throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                }
                this.trackBar_phase_ch2.Value = (int)(val * 10);
                phase_ch2 = val;

            }
            catch (Exception ex)
            {
            }
            finally
            {
                this.textBox_phase_ch2.Text = phase_ch2.ToString();
            }
        }



        private void trackBar_duty_ch1_ValueChanged(object sender, EventArgs e)
        {
            duty_ch1 = ((double)(this.trackBar_duty_ch1.Value) / 10);
            this.textBox_duty_ch1.Text = duty_ch1.ToString();
        }

        private void textBox_duty_ch1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_duty_ch1();
            }
        }

        private void textBox_duty_ch1_Leave(object sender, EventArgs e)
        {
            validate_text_duty_ch1();
        }

        private void validate_text_duty_ch1()
        {
            try
            {
                Double val = Double.Parse(this.textBox_duty_ch1.Text);
                if (val > 100)
                {
                    throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                }
                this.trackBar_duty_ch1.Value = (int)(val * 10);
                duty_ch1 = val;

            }
            catch (Exception ex)
            {
            }
            finally
            {
                this.textBox_duty_ch1.Text = duty_ch1.ToString();
            }
        }



        private void trackBar_dut_ch2_ValueChanged(object sender, EventArgs e)
        {
            duty_ch2 = ((double)(this.trackBar_duty_ch2.Value) / 10);
            this.textBox_duty_ch2.Text = duty_ch2.ToString();
        }

        private void textBox_duty_ch2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_duty_ch2();
            }
        }

        private void textBox_duty_ch2_Leave(object sender, EventArgs e)
        {
            validate_text_duty_ch2();
        }

        private void validate_text_duty_ch2()
        {
            try
            {
                Double val = Double.Parse(this.textBox_duty_ch2.Text);
                if (val > 100)
                {
                    throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                }
                this.trackBar_duty_ch2.Value = (int)(val * 10);
                duty_ch1 = val;

            }
            catch (Exception ex)
            {
            }
            finally
            {
                this.textBox_duty_ch2.Text = duty_ch2.ToString();
            }
        }


        private void trackBar_offset_ch1_ValueChanged(object sender, EventArgs e)
        {
            offset_ch1 = ((double)(this.trackBar_offset_ch1.Value));
            this.textBox_offset_ch1.Text = offset_ch1.ToString();
        }

        private void textBox_offset_ch1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_offset_ch1();
            }
        }

        private void textBox_offset_ch1_Leave(object sender, EventArgs e)
        {
            validate_text_offset_ch1();
        }

        private void validate_text_offset_ch1()
        {
            try
            {
                Double val = Double.Parse(this.textBox_offset_ch1.Text);
                if (val > device.genCfg.VRef)
                {
                    throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                }
                this.trackBar_offset_ch1.Value = (int)(val);
                offset_ch1 = val;
            }
            catch (Exception ex)
            {
            }
            finally
            {
                this.textBox_offset_ch1.Text = offset_ch1.ToString();
            }
        }


        private void trackBar_offset_ch2_ValueChanged(object sender, EventArgs e)
        {
            offset_ch2 = ((double)(this.trackBar_offset_ch2.Value));
            this.textBox_offset_ch2.Text = offset_ch2.ToString();
        }

        private void textBox_offset_ch2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                validate_text_offset_ch2();
            }
        }

        private void textBox_offset_ch2_Leave(object sender, EventArgs e)
        {
            validate_text_offset_ch2();
        }

        private void validate_text_offset_ch2()
        {
            try
            {
                Double val = Double.Parse(this.textBox_offset_ch2.Text);
                if (val > device.genCfg.VRef)
                {
                    throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                }
                this.trackBar_offset_ch2.Value = (int)(val);
                offset_ch2 = val;
            }
            catch (Exception ex)
            {
            }
            finally
            {
                this.textBox_offset_ch2.Text = offset_ch2.ToString();
            }
        }


        private void toolStripTextBox_signal_leng_TextChanged(object sender, EventArgs e)
        {
            try
            {
                int val = int.Parse(this.toolStripTextBox_signal_leng.Text);
                //if(val>device.genCfg.BufferLength/2)
                if ((actual_channels == 1 && val > device.genCfg.BufferLength / 2) || (actual_channels == 2 && val > device.genCfg.BufferLength / 2 / 2))
                {
                    signal_leng = device.genCfg.BufferLength / 2 / actual_channels;
                    //throw new System.ArgumentException("Parameter cannot be greather then ", "original");
                }
                else
                {
                    signal_leng = val;
                }
                toolStripTextBox_signal_leng.Text = val.ToString();
                
                if (signal_leng < 10) {
                    signal_leng = 100;
                }
                //signal_leng_ch2 = val;
            }
            catch (Exception ex)
            {
            }
        }

        private void checkBox_enable_ch2_CheckedChanged(object sender, EventArgs e)
        {
            takeGenSemaphore(5100);
            actual_channels = checkBox_enable_ch2.Checked ? 2 : 1;
            validate_control_ch2();
            if (!bestFreqFit && !customLeng)
            {
                signal_leng_ch2 = device.genCfg.BufferLength / 2 / actual_channels;
            }
            giveGenSemaphore();
        }

        private void validate_control_ch2() {
            zedGraphControl_gen_ch2.Enabled = actual_channels == 2 ? true : false;
            trackBar_ampl_ch2.Enabled = actual_channels == 2 ? true : false;
            trackBar_duty_ch2.Enabled = actual_channels == 2 ? true : false;
            trackBar_freq_ch2.Enabled = actual_channels == 2 ? true : false;
            trackBar_offset_ch2.Enabled = actual_channels == 2 ? true : false;
            trackBar_phase_ch2.Enabled = actual_channels == 2 ? true : false;

            textBox_ampl_ch2.Enabled = actual_channels == 2 ? true : false;
            textBox_duty_ch2.Enabled = actual_channels == 2 ? true : false;
            textBox_freq_ch2.Enabled = actual_channels == 2 ? true : false;
            textBox_offset_ch2.Enabled = actual_channels == 2 ? true : false;
            textBox_phase_ch2.Enabled = actual_channels == 2 ? true : false;
            checkBox_khz_ch2.Enabled = actual_channels == 2 ? true : false;

            label_real_freq_ch2_title.Enabled = actual_channels == 2 ? true : false;
            label_real_freq_ch2.Enabled = actual_channels == 2 ? true : false;
            label_sig_leng_ch2.Enabled = actual_channels == 2 ? true : false;
            label9.Enabled = actual_channels == 2 ? true : false;

            radioButton_arb_ch2.Enabled = actual_channels == 2 ? true : false;
            radioButton_saw_ch2.Enabled = actual_channels == 2 ? true : false;
            radioButton_sine_ch2.Enabled = actual_channels == 2 ? true : false;
            radioButton_square_ch2.Enabled = actual_channels == 2 ? true : false;

        }

        private void checkBox_join_frequencies_CheckedChanged(object sender, EventArgs e)
        {
            frequencyJoin = this.checkBox_join_frequencies.Checked;
            if (frequencyJoin) {
                trackBar_freq_ch2.Value = trackBar_freq_ch1.Value;
            }
        }

        private void radioButton_sine_ch1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_sine_ch1.Checked)
            {
                takeGenSemaphore(5001);
                signalType_ch1 = SIGNAL_TYPE.SINE;
                validateArbLength();
                giveGenSemaphore();
            }
        }

        private void radioButton_square_ch1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_square_ch1.Checked)
            {
                takeGenSemaphore(5002);
                signalType_ch1 = SIGNAL_TYPE.SQUARE;
                validateArbLength();
                giveGenSemaphore();
            }
        }

        private void radioButton_saw_ch1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_saw_ch1.Checked)
            {
                takeGenSemaphore(5003);
                signalType_ch1 = SIGNAL_TYPE.SAW;
                validateArbLength();
                giveGenSemaphore();
            }
        }

        private void radioButton_arb_ch1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_arb_ch1.Checked)
            {
                takeGenSemaphore(5004);
                signal_leng_ch1 = arb_signal_ch1.Length;
                signalType_ch1 = SIGNAL_TYPE.ARB;
                validateArbLength();
                giveGenSemaphore();
            }
        }

        private void radioButton_arb_ch2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_arb_ch2.Checked)
            {
                takeGenSemaphore(5005);
                signal_leng_ch2 = arb_signal_ch2.Length;
                signalType_ch2 = SIGNAL_TYPE.ARB;
                validateArbLength();
                giveGenSemaphore();
            }
        }

        private void radioButton_sine_ch2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_sine_ch2.Checked)
            {
                takeGenSemaphore(5006);
                signalType_ch2 = SIGNAL_TYPE.SINE;
                validateArbLength();
                giveGenSemaphore();
            }
        }

        private void radioButton_square_ch2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_square_ch2.Checked)
            {

                takeGenSemaphore(5007); 
                signalType_ch2 = SIGNAL_TYPE.SQUARE;
                validateArbLength();
                giveGenSemaphore();
            }
        }

        private void radioButton_saw_ch2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_saw_ch2.Checked)
            {
                takeGenSemaphore(5008);
                signalType_ch2 = SIGNAL_TYPE.SAW;
                validateArbLength();
                giveGenSemaphore();
            }
        }


        private void validateArbLength() {
            
            button_load_ch1.Enabled = signalType_ch1 == SIGNAL_TYPE.ARB ? true : false;
            //radioButton_arb_ch2.Checked = signalType_ch1 == SIGNAL_TYPE.ARB ? true : false;
            //maximumPossibleToolStripMenuItem.Enabled = signalType_ch1 == SIGNAL_TYPE.ARB ? false : true;
            //maximumPossibleToolStripMenuItem.Checked = signalType_ch1 == SIGNAL_TYPE.ARB ? false : true;
            //bestFreqFitToolStripMenuItem.Enabled = signalType_ch1 == SIGNAL_TYPE.ARB ? false : true;
            //bestFreqFitToolStripMenuItem.Checked = signalType_ch1 == SIGNAL_TYPE.ARB ? false : true;
            //customToolStripMenuItem.Enabled = signalType_ch1 == SIGNAL_TYPE.ARB ? false : true;
            //customToolStripMenuItem.Checked = signalType_ch1 == SIGNAL_TYPE.ARB ? true : false;
            //toolStripTextBox_signal_leng.Enabled = signalType_ch1 == SIGNAL_TYPE.ARB ? false : true;
            //customLeng = signalType_ch1 == SIGNAL_TYPE.ARB ? true : false;
            //bestFreqFit = signalType_ch1 == SIGNAL_TYPE.ARB ? false : true;
        }


        // END track-bar and tex-box functions


        private void button_gen_control_Click(object sender, EventArgs e)
        {
            if (button_gen_control.Text.Equals("Enable") && button_gen_control.Enabled==true)
            {
                this.button_gen_control.Enabled = false;
                label_status.BackColor = Color.Yellow;
                label_status_gen.Text = "Updating";
                //send data to generator;
                if (actual_channels == 2) { 
                    set_data_length(Commands.DATA_LENGTH_CH1, signal_leng_ch1);
                    set_data_length(Commands.DATA_LENGTH_CH2, signal_leng_ch2);
                    set_num_of_channels(Commands.CHANNELS_2);
                }else{
                    set_num_of_channels(Commands.CHANNELS_1);
                    set_data_length(Commands.DATA_LENGTH_CH1, signal_leng_ch1);
                }


                if (checkBox_khz_ch1.Checked)
                {
                    set_frequency((int)(device.systemCfg.PeriphClock / signal_leng_ch1 / divider_ch1 * signal_leng_ch1), 1);
                }
                else {
                    set_frequency((int)(device.systemCfg.PeriphClock / signal_leng_ch1 / divider_ch1 * signal_leng_ch1), 1);
                }

                if (checkBox_khz_ch2.Checked && actual_channels==2)
                {
                    set_frequency((int)(device.systemCfg.PeriphClock / signal_leng_ch2 / divider_ch2 * signal_leng_ch2), 2);
                }
                else if (actual_channels == 2)
                {
                    set_frequency((int)(device.systemCfg.PeriphClock / signal_leng_ch2 / divider_ch2 * signal_leng_ch2), 2);
                }

                toSend = signal_ch1.Length;
                sent = 0;
                index = 0;
                actualSend = 0;
                sendingChannel = 1;
                dataSendingTimer.Start();

                Thread.Sleep(10);
                send_next(signal_ch1, 1);

                
                

            }
            else {
                gen_stop();
                this.button_gen_control.Text = "Enable";
                this.label_status_gen.Text = "Idle";
                label_status.BackColor = Color.Red;
                generating = false;
            }
        }

        private void button_load_ch1_Click(object sender, EventArgs e)
        {
            // Create an instance of the open file dialog box.
            OpenFileDialog openFileDialog = new OpenFileDialog();
            ArbDialog ArbSignalDialog = new ArbDialog(device.genCfg.VRef, device.genCfg.dataDepth);

            // Set filter options and filter index.
            openFileDialog.Filter = "CSV Files (.csv)|*.csv|Text Files (.txt)|*.txt|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            openFileDialog.Multiselect = false;

            // Call the ShowDialog method to show the dialog box.
            DialogResult userClickedOK = openFileDialog.ShowDialog();
            DialogResult ArbDialogOK = DialogResult.Cancel;
            if (userClickedOK.Equals(DialogResult.OK))
            {
                ArbDialogOK = ArbSignalDialog.ShowDialog();
            }

            // Process input if the user clicked OK.
            if (userClickedOK.Equals(DialogResult.OK) && ArbDialogOK.Equals(DialogResult.OK))
            {
                // Open the selected file to read.
                StreamReader fileStream=null;
                string input;
                string[] values;
                List<double> ch1 = new List<double>();
                List<double> ch2 = new List<double>();
                int numOfCh = 0;

                try
                {
                    fileStream = new StreamReader(openFileDialog.FileName);
                    do
                    {
                        input = fileStream.ReadLine();
                        Console.WriteLine(input);

                        if (input != null)
                        {
                            values = input.Split(ArbSignalDialog.GetSeparator());
                            if (numOfCh == 0)
                            {
                                numOfCh = values.Length;
                            }
                            for (int i = 0; i < values.Length; i++)
                            {
                                if (i == 0)
                                {
                                    ch1.Add(double.Parse(values[i]));
                                }
                                else
                                {
                                    ch2.Add(double.Parse(values[i]));
                                }
                            }
                        }
                    }
                    while (input != null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error during opening file\r\n" + ex, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if (fileStream != null)
                {
                    fileStream.Close();
                }


                if (ch1.Count > 0)
                {
                    arb_signal_ch1 = new double[ch1.Count];
                    signal_leng_ch1 = ch1.Count;
                    if (signal_leng_ch1 > device.genCfg.BufferLength / numOfCh) {
                        signal_leng_ch1 = device.genCfg.BufferLength / numOfCh;
                        MessageBox.Show("Signal for ch1 is too long \r\nIt will be truncated\r\n", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    
                        //throw new Exception("Signal je prilis dlouhy");
                    }
                    int i = 0;
                    foreach (var item in ch1)
                    {
                        arb_signal_ch1[i] = item * device.genCfg.VRef / 1000 / ArbSignalDialog.GetMaxValue();
                        i++;
                    }
                }

                if (ch2.Count > 0)
                {
                    arb_signal_ch2 = new double[ch2.Count];
                    signal_leng_ch2 = ch2.Count;
                    if (signal_leng_ch2 > device.genCfg.BufferLength / numOfCh)
                    {
                        signal_leng_ch2 = device.genCfg.BufferLength / numOfCh;
                        MessageBox.Show("Signal for ch2 is too long \r\nIt will be truncated\r\n", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                        //throw new Exception("Signal je prilis dlouhy");
                    }
                    int i = 0;
                    foreach (var item in ch2)
                    {
                        arb_signal_ch2[i] = item * device.genCfg.VRef / 1000 / ArbSignalDialog.GetMaxValue();
                        i++;
                    }
                }
                this.toolStripTextBox_signal_leng.Text = signal_leng_ch1.ToString();
                openFileDialog.Dispose();
                ArbSignalDialog.Dispose();                
            }
        }

        public bool takeGenSemaphore(int ms)
        {
            bool result = false;
            result = genSemaphore.WaitOne(ms);
            if (!result)
            {
                throw new Exception("Unable to take semaphore");
            }
            return result;
        }

        public void giveGenSemaphore()
        {
            genSemaphore.Release();
        }

        private void outputBufferToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.outputBufferToolStripMenuItem.Checked)
            {
                this.outputBufferToolStripMenuItem.Checked = false;
                gen_unset_out_buff();
            }
            else {
                this.outputBufferToolStripMenuItem.Checked = true;
                gen_set_out_buff();
            }
        }






    }
}
