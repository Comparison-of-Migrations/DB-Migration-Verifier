import AppKit
import Foundation
import CryptoKit
import ApplicationServices
func fail(_ s:String)->Never{fputs(s+"\n",stderr);exit(1)}
let args=CommandLine.arguments,root="/Users/youptebook/workspace/DB-Migration-Verifier/"
guard args.count==4,args[1].hasPrefix(root+"artifacts/replica/workers/root/pageintegration/"),args[1].hasSuffix("/build.json"),args[2].hasPrefix(root+"artifacts/replica/workers/root/pageintegration/")else{fail("Exact owned manifest/output required")}
let manifest=URL(fileURLWithPath:args[1]),metadata=try JSONSerialization.jsonObject(with:Data(contentsOf:manifest))as![String:Any]
guard metadata["owner"]as?String=="root",metadata["component"]as?String=="pageintegration",metadata["runtime"]as?String=="osx-arm64",metadata["buildExitCode"]as?Int==0 else{fail("Owned successful Mac build required")}
let lease=try JSONSerialization.jsonObject(with:Data(contentsOf:URL(fileURLWithPath:root+"docs/replica/ENVIRONMENT_LEASE.json")))as![String:Any]
guard ((lease["resources"]as![String:Any])["macos"]as![String:Any])["owner"]as?String=="root"else{fail("Root Mac lease required")}
let bundle=manifest.deletingLastPathComponent().appendingPathComponent("PageIntegrationFullFlowCheck.app"),binary=bundle.appendingPathComponent("Contents/MacOS/ReplicaProbe.dll")
let hash=SHA256.hash(data:try Data(contentsOf:binary)).map{String(format:"%02x",$0)}.joined()
guard hash==metadata["binarySha256"]as?String,!NSWorkspace.shared.runningApplications.contains(where:{$0.bundleURL==bundle}) else{fail("Fresh frozen owned bundle required")}
let config=NSWorkspace.OpenConfiguration();config.arguments=["--pageintegration-window",args[2]+"/cases.json",args[2],"--flow-window-only"];if args[3]=="en"{config.arguments.append("--flow-english")};config.environment=["DOTNET_ROOT":"/Users/youptebook/.cache/db-migration-verifier/dotnet","DBMV_HEADLESS":"0"];config.createsNewApplicationInstance=true
var finished=false
NSWorkspace.shared.openApplication(at:bundle,configuration:config){app,error in
 guard let app,error==nil,app.bundleURL==bundle else{fail("Owned file scope launch failed: "+String(describing:error))}
 let record:[String:Any]=["pid":app.processIdentifier,"bundle":bundle.path,"binarySha256":hash,"arguments":config.arguments,"output":args[2],"manifest":manifest.path,"scope":"Actual App whole-flow physical input preparation; no navigation success inferred from launch"]
 do{try JSONSerialization.data(withJSONObject:record,options:[.prettyPrinted,.sortedKeys]).write(to:URL(fileURLWithPath:args[2]+"/current-native.json"))}catch{fail("Could not preserve owned launch record")}
 print(app.processIdentifier);finished=true
}
while !finished{RunLoop.current.run(until:Date(timeIntervalSinceNow:0.05))}
