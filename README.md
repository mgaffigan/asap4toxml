# Asap42ToXml

Convert a file in the [ASAP PDMP 4.x](https://www.asapnet.org/pmp-implementation-guides.html) format to an XML file.

Command:

    Asap42ToXml.exe infile.dat outfile.xml

Input file (with line-breaks added for clarity):

	TH*0.0*F00B0BE000D000E0B0B00B0EAF0B0D0D*00**00000000*000000*P**\\
	IS*0000000000*Example Pharmacy*#00000000#-#00000000#\
	PHA*0000000000*0000000*BI0000000*Example Pharmacy*0000 Main Street**Nowhere*IN*00000*0000000000*Smith,John*\
	PAT*******Report*Zero***************\
	DSP*****00000000****************\
	PRE**\
	TP*0\
	TT*F00B0BE000D000E0B0B00B0EAF0B0D0D*0\

Output file:

	<?xml version="1.0" encoding="utf-8"?>
	<Asap4File>
	  <TH TH01="0.0" TH02="F00B0BE000D000E0B0B00B0EAF0B0D0D" TH03="00" TH05="00000000" TH06="000000" TH07="P" />
	  <IS IS01="0000000000" IS02="Example Pharmacy" IS03="#00000000#-#00000000#" />
	  <PHA PHA01="0000000000" PHA02="0000000" PHA03="BI0000000" PHA04="Example Pharmacy" PHA05="0000 Main Street" PHA07="Nowhere" PHA08="IN" PHA09="00000" PHA10="0000000000" PHA11="Smith,John" />
	  <PAT PAT07="Report" PAT08="Zero" />
	  <DSP DSP05="00000000" />
	  <PRE />
	  <TP TP01="0" />
	  <TT TT01="F00B0BE000D000E0B0B00B0EAF0B0D0D" TT02="0" />
	</Asap4File>

This is a syntax only transform - no semantic interpretation is made.  Testing has shown success with low memory usage up to files of several gigabytes.