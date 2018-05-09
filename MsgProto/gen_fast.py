import os
import json
import zlib
from hashlib import md5

#func
def fileMd5(filePath):
    try:
        with open(filePath, 'rb') as fp:
            m = md5()
            m.update(fp.read())
            curmd5 = m.hexdigest()
            return curmd5
    except IOError, arg:
        return ""


#cacheFile = "cache.json"
#protoDir = "protos/"
#outDir = "../GameEditor/Assets/Script/GameCore/Resource/PB/"


def genSource(protoDir, outDir, cacheFile):
    #load
    try:
        with open(cacheFile, 'r') as fp:
            cache = json.load(fp)
    except IOError, arg:
        cache = {}

    #deal proto files
    protoNames = os.listdir(protoDir)
    for protoName in protoNames:
        protoFile = protoDir + protoName
        protoMd5 = fileMd5(protoFile)

        csName = os.path.basename(protoName).replace(".proto", ".cs")
        csFile = outDir + csName
        csMd5 = fileMd5(csFile)
         
        cacheItem = cache.get(protoFile)

        # need gen
        if cacheItem == None or cacheItem["protoMd5"] != protoMd5 or cacheItem["csMd5"] != csMd5:
            str = "tool\protogen -i:" + protoFile + " -o:" + csFile + " -p:observable=true"
            os.system(str)
            cacheItem = {"protoMd5":protoMd5, "csMd5":csMd5}
            cache[protoFile] = cacheItem
            print(protoFile, cacheItem)
            

    #save
    with open(cacheFile, 'w') as fp:
        json.dump(cache, fp)

