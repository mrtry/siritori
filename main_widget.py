#!/usr/bin/python
# -*- coding: utf-8 -*-

import sys
import jtalk
import siritori
import codecs

from PyQt4 import QtGui
from PyQt4 import QtCore

class Window(QtGui.QWidget):
    def __init__(self):
        super(Window, self).__init__()
        self.initUI()
        
    def initUI(self):
	self.talk_text = QtGui.QLineEdit(self)        
	self.call_area = QtGui.QTextEdit(self)

        self.talkButton = QtGui.QPushButton("talk")
        self.talkButton.clicked.connect(self.talk)
	 
	vbox = QtGui.QVBoxLayout()
        vbox.addStretch(1)

		
	vbox.addWidget(self.call_area)
	vbox.addWidget(self.talk_text)
        vbox.addWidget(self.talkButton)

        self.setLayout(vbox)    
        self.setGeometry(300, 300, 300, 150)
        self.setWindowTitle('main_window')    
        self.show()

    def call(self):
        post = str(self.talk_text.text())
        enc_post = post.decode("utf-8")
        print enc_post

    def talk(self): 
        post = unicode(self.talk_text.text()).rstrip()
        post = post.encode("utf-8")
	
        say = QtCore.QString.fromLocal8Bit("[あなた]:") + self.talk_text.text()
        self.call_area.append(say)
 
        f = open("talk.txt","w")
        f.write(post)
        f.close()

	jtalk.execute("cat talk.txt")
        jtalk.execute("nkf -w8Lu --overwrite talk.txt")
	siritori.siri("talk.txt")

        f = open("talk.txt","r")
        post = unicode(self.talk_text.text()).rstrip()
        post = post.encode("utf-8")
	
        say = QtCore.QString.fromLocal8Bit("[Mei]:") + QtCore.QString.fromLocal8Bit((f.readline()).rstrip())
        self.call_area.append(say)        
        f.close()	

        jtalk.talk()
	jtalk.execute("cat talk.txt")

def main():    
    app = QtGui.QApplication(sys.argv)
    ex = Window()
    sys.exit(app.exec_())


if __name__ == '__main__':
    main()
