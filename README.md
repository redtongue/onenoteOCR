# onenoteOCR
**利用onenote做图像文字识别的应用，并且加上了近义词搜索**

运行界面如下：

![main](https://github.com/redtongue/onenoteOCR/blob/master/image/main.png)

1. 在本地窗口那一栏可以选择需要扫描的图片，如图所示，选择的是一个普通的程序截图，字体大小一般，

2. 在输出目录中选择存储扫描结果的目录，会以txt的格式保存下来


> 在输出目录的下方会显示扫描结果，如结果所示，准确率是很高的，错误只有“搜”，其主要原因是，ocr的基本原理是对图像二值化之后，找到图像中的连通域，再对连通域中的信息匹配识别，但是“搜”的左右两部分是不连通的，所以很容易造成识别错误。

3. 在搜索词框中，可以对关键搜索，不仅仅是简单匹配，是近义词的同等检索，即，搜索结果为搜索词在ocr结果中所有的近义词。

## 搜索实现

具体做法如下，首先需要一个近义词词库（包含两个表，其中一个包含每一类近义词及其对应id，另外一个包含每一类相同unicode编码的词及其Unicode码），定义一个hash函数，输入为搜索词，输出为unicode的变换值（不直接是unicode值的原因是，每个unicode值对应的词扫，导致映射稀疏），根据hash值便可以快速找到改搜索词的所有同义词。

# tesseract-ocr

经过测试发现，onenote的ocr结果要比tesseract的要好一些，但是tesseract提供训练接口，且开源，在项目上使用更加方便

## 安装tesseract

下载地址：

源代码：[https://github.com/tesseract-ocr/tesseract](https://github.com/tesseract-ocr/tesseract "https://github.com/tesseract-ocr/tesseract")

可执行文件：[http://download.csdn.net/download/whatday/7740469](http://download.csdn.net/download/whatday/7740469)

## 使用tesseract

    tesseract filename result -l font

如上代码所示，filename为目标图片地址，result为结果存储目录，font为字体，可以为eng（英文），chi_sim（中文），或其他自己训练的文件名

## 训练

### 生成box文件

    tesseract hanzi.normal.exp0.jpg hanzi.normal.exp0 -l chi_sim batch.nochop makebox

lang为语言名称，但是我随便试一个也不影响，normal是字体名称，修改位自己想用的名字，exp0对应的图片序号（exp1，exp2等等），chi_sim 基于已有的字体chi_sim训练

在目标文件夹内生成一个名为font_properties的文本文件，内容为：

    normal 0 0 0 0 0

normal为字体名称

### 矫正

打开jTessBoxEditor，BOX Editor -> Open，打开.box文件

![https://github.com/redtongue/onenoteOCR/blob/master/image/tesseract.png](https://github.com/redtongue/onenoteOCR/blob/master/image/tesseract.png) 

矫正的目的是为了避免上面onenote连通域的问题，手动将左右分离的汉字重新组合。

### 训练生成tr文件

	tesseract hanzi.normal.exp0.jpg hanzi.normal.exp0 nobatch box.train

训练生成tr文件是为了保存之前的矫正

### 计算字符集

	unicharset_extractor hanzi.normal.exp0.box

计算字符集,从生成的box文件中提取。

	mftraining -F font_properties -U unicharset hanzi.normal.exp0.tr

### 生成字体

聚集tesseract 识别的训练文件

	cntraining hanzi.normal.exp0.tr

把目录下的unicharset、inttemp、pffmtable、shapetable、normproto这五个文件前面都加上normal.。（注意是“normal.”）

合并得到normal.traineddata

	combine_tessdata normal.

最后将normal.trainddata复制到Tesseract-OCR中tessdata文件夹即可。

	tesseract filename result -l normal






 


