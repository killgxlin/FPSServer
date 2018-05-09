#import gen_source
#
#gen_source.genSource("protos", "../GameEditor/Assets/Script/GameCore/Resource/PB")
#gen_source.genSource("data", "../GameEditor/Assets/Script/GameCore/Resource/Data/PB")

import gen_fast
#gen_fast.genSource("protos/", "../FPSServer/MsgProto/", "cache_server.json")
#gen_fast.genSource("protos/", "../FPSClient/MsgProto/", "cache_client.json")
#gen_fast.genSource("data/", "../FPSServer/DataProto/", "cache_server.json")
#gen_fast.genSource("data/", "../FPSClient/DataProto/", "cache_client.json")
gen_fast.genSource("protos/", "../Share/MsgProto/", "cache_share.json")


print("gen ok!!!!!!!!!!!!")
raw_input()
