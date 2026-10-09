// Official AX actions confined to the owned footer verification app.
import AppKit
import ApplicationServices
import Foundation
func fail(_ s:String)->Never{fputs(s+"\n",stderr);exit(1)}
func attr(_ e:AXUIElement,_ n:String)->CFTypeRef?{var v:CFTypeRef?;return AXUIElementCopyAttributeValue(e,n as CFString,&v) == .success ? v:nil}
func text(_ e:AXUIElement,_ n:String)->String{attr(e,n)as?String ?? ""}
let args=CommandLine.arguments
let allowed=["FooterCase-connect","FooterCase-connect-busy","FooterCase-select-none","FooterCase-trial-ready","FooterLocale","FooterRecord","FooterClose","Footer-connect","Footer-back","Footer-trial-back","Footer-review"]
guard args.count==2,allowed.contains(args[1])else{fail("Fixed footer control required")}
let apps=NSRunningApplication.runningApplications(withBundleIdentifier:"local.dbmv.replica.actionfootercheck")
guard apps.count==1,let app=apps.first,app.bundleURL?.path=="/Users/youptebook/workspace/DB-Migration-Verifier/artifacts/replica/ActionFooterCheck.app"else{fail("Unique owned footer app required")}
let windows=attr(AXUIElementCreateApplication(app.processIdentifier),kAXWindowsAttribute)as?[AXUIElement] ?? []
let owned=windows.filter{text($0,kAXTitleAttribute)=="Replica ActionFooter verification"}
guard owned.count==1 else{fail("Unique owned footer window required")}
var targets:[AXUIElement]=[]
func visit(_ e:AXUIElement,_ d:Int){guard d<30 else{return};if text(e,kAXIdentifierAttribute)==args[1]{targets.append(e)};for c in attr(e,kAXChildrenAttribute)as?[AXUIElement] ?? []{visit(c,d+1)}}
visit(owned[0],0);guard targets.count==1 else{fail("Unique footer target required")}
guard attr(targets[0],kAXEnabledAttribute)as?Bool==true else{print("Disabled",args[1]);exit(0)}
guard AXUIElementPerformAction(targets[0],kAXPressAction as CFString) == .success else{fail("Owned AX press refused")}
RunLoop.current.run(until:Date(timeIntervalSinceNow:0.2));print("Pressed",args[1])
