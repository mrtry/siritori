# -*- coding: utf-8 -*-
import sys
import jtalk
import random
import codecs
import boo

def siri(text):
	f = codecs.open("talk.txt","r","utf-8")
        in_text = f.readline()
        in_text = in_text.rstrip()
        f.close()

        in_first = in_text[:2]
        in_first = in_first.encode("utf-8")
	first_dec= in_first.decode("utf-8")

        print "in_first"
        print type(in_first)
	print in_first	

	in_last = in_text[len(in_text)-1:len(in_text)]

	f = codecs.open("last_word.txt","r","utf-8")
	last_word = f.readline()
	last_word = last_word.encode("utf-8")
	f.close()

	print "last_word"
        print type(last_word)
	print last_word

	s1 = last_word + ""
	s2 = in_first + ""
	
	boo.boolean(s1, s2)
	if(s1 not in s2):
		cmd = "「%s」からはじめてください。"% last_word
		f = open("talk.txt","w")
		f.write(cmd)
		f.close()
		return		

	danger_word = "ん"
	danger_word = danger_word.decode("utf-8")

	if (in_last == danger_word):
		f = open("talk.txt","w")
		cmd = "「ん」がつきましたね。あなたの負けです。"
		f.write(cmd)
		f.close()
		return

	in_last = in_last.encode("utf-8")
	
	dic_text = "./dic/%s.txt" %in_last	
	line_count = sum(1 for line in open(dic_text))
	rand = random.randint(0,line_count) -1

	f = codecs.open(dic_text,"r","utf-8")
	out_text = f.readlines()[rand]
	out_text = out_text.rstrip()
	f.close()

	out_first = out_text[:1]
	out_last = out_text[len(out_text)-1:len(out_text)]	

	buf = out_last

	out_text = out_text.encode("utf-8")
	out_last = out_last.encode("utf-8")
	f = open("talk.txt","w")
	cmd = "「%s」。次は「%s」です。" % (out_text, out_last)
	f.write(cmd)
	f.close()

	if (buf == danger_word):
		f = open("talk.txt","a")
		cmd = "「ん」がつきましたね。私の負けです。"
		f.write(cmd)
		f.close()
		return 

	f = open("last_word.txt","w")
	f.write(out_last)
	f.close()
		
