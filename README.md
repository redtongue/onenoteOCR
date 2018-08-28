# onenoteOCR
利用onenote做图像文字识别的应用，并且加上了近义词搜索

运行界面如下：

![main](https://github.com/redtongue/onenoteOCR/blob/master/image/main.png)

在本地窗口那一栏可以选择需要扫描的图片，如图所示，选择的是一个普通的程序截图，字体大小一般，

在输出目录中选择存储扫描结果的目录，会以txt的格式保存下来

在输出目录的下方会显示扫描结果，如结果所示，准确率是很高的，错误只有“搜”，其主要原因是，ocr的基本原理是对图像二值化之后，找到图像中的连通域，再对连通域中的信息匹配识别，但是“搜”的左右两部分是不连通的，所以很容易造成识别错误。

在搜索词框中，可以对关键搜索，不仅仅是简单匹配，是近义词的同等检索，即，搜索结果为搜索词在ocr结果中所有的近义词。

具体做法如下，首先需要一个近义词词库（包含两个表，其中一个包含每一类近义词及其对应id，另外一个包含每一类相同unicode编码的词及其Unicode码），定义一个hash函数，输入为搜索词，输出为unicode的变换值（不直接是unicode值的原因是，每个unicode值对应的词扫，导致映射稀疏），根据hash值便可以快速找到改搜索词的所有同义词。