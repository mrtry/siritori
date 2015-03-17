set VOICE=hts_voice_nitech_jp_atr503_m001\nitech_jp_atr503_m001
rem set VOICE=mei_normal
..\bin\open_jtalk -x ..\dic -m ..\voice\%VOICE%.htsvoice -z 6000 ..\text.txt
