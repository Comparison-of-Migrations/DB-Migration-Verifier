// User-authorized CGEvent path: only the two owned NumberInput fixtures.
import AppKit
import ApplicationServices
import Foundation
import ScreenCaptureKit

var releasePoint: CGPoint?
func fail(_ message: String) -> Never {
    if let point = releasePoint {
        CGEvent(mouseEventSource: nil, mouseType: .leftMouseUp, mouseCursorPosition: point, mouseButton: .left)?.post(tap: .cghidEventTap)
        releasePoint = nil
    }
    fputs(message + "\n", stderr); exit(1)
}
let args = CommandLine.arguments
guard args.count == 4, ["source", "native"].contains(args[1]),
      ["inspect", "hover", "hold", "pressed", "click"].contains(args[2]), ["up", "down"].contains(args[3]) else {
    fail("Expected source/native inspect/hover/hold/pressed/click up/down")
}
let target = args[1], mode = args[2], half = args[3]
let bundle = target == "source" ? "com.google.Chrome" : "com.dbmigrationverifier.numberinputcheck"
let applications = NSRunningApplication.runningApplications(withBundleIdentifier: bundle)
guard applications.count == 1, let application = applications.first else { fail("Owned application must be unique and already running") }
if target == "source" {
    guard application.bundleURL?.path == "/Applications/Google Chrome.app" else { fail("Unexpected Chrome path") }
} else {
    guard application.bundleURL?.path == "/Users/youptebook/workspace/DB-Migration-Verifier/artifacts/replica/NumberInput Check.app" else { fail("Unexpected owned verifier path") }
}
func attribute(_ element: AXUIElement, _ name: String) -> CFTypeRef? {
    var value: CFTypeRef?
    guard AXUIElementCopyAttributeValue(element, name as CFString, &value) == .success else { return nil }
    return value
}
func string(_ element: AXUIElement, _ name: String) -> String { attribute(element, name) as? String ?? "" }
let root = AXUIElementCreateApplication(application.processIdentifier)
let windows = attribute(root, kAXWindowsAttribute) as? [AXUIElement] ?? []
let owned = windows.filter { window in
    let title = string(window, kAXTitleAttribute)
    return target == "source" ? title.hasPrefix("NumberInput · original isolation") : title == "Replica NumberInput verification"
}
guard owned.count == 1, let window = owned.first else {
    fail("Owned window must be unique (AX trusted=\(AXIsProcessTrusted()), windows=\(windows.count), matches=\(owned.count))")
}
if target == "source" {
    guard string(window, kAXDocumentAttribute) == "http://10.211.55.2:8768/numberinput.html" else { fail("Owned source URL mismatch") }
}
var fields: [AXUIElement] = []
func visit(_ element: AXUIElement, _ depth: Int) {
    guard depth < 30 else { return }
    if string(element, kAXRoleAttribute) == "AXIncrementor" { fields.append(element); return }
    for child in attribute(element, kAXChildrenAttribute) as? [AXUIElement] ?? [] { visit(child, depth + 1) }
}
visit(window, 0)
guard fields.count == 6 else { fail("Expected exactly six owned fixture number fields") }
func rectangle(_ element: AXUIElement) -> CGRect? {
    guard let p = attribute(element, kAXPositionAttribute), let s = attribute(element, kAXSizeAttribute),
          CFGetTypeID(p) == AXValueGetTypeID(), CFGetTypeID(s) == AXValueGetTypeID() else { return nil }
    var point = CGPoint.zero, size = CGSize.zero
    guard AXValueGetValue(p as! AXValue, .cgPoint, &point), AXValueGetValue(s as! AXValue, .cgSize, &size) else { return nil }
    return CGRect(origin: point, size: size)
}
fields.sort { (rectangle($0)?.minY ?? 0) < (rectangle($1)?.minY ?? 0) }
let field = fields[0]
guard let rect = rectangle(field), abs(rect.width - 112) < 0.01, abs(rect.height - 44) < 0.01 else { fail("Unexpected owned number geometry") }
if target == "native" {
    guard string(field, kAXTitleAttribute) == "number-default" else { fail("Owned native field mismatch") }
}
guard let windowRect = rectangle(window) else { fail("Owned window rectangle unavailable") }
let metadata: [String: Any] = ["target": target, "title": string(window, kAXTitleAttribute), "pid": application.processIdentifier,
    "windowRect": ["x": windowRect.minX, "y": windowRect.minY, "width": windowRect.width, "height": windowRect.height],
    "rect": ["x": rect.minX, "y": rect.minY, "width": rect.width, "height": rect.height], "api": "CGEvent", "mode": mode, "half": half]
print(String(data: try JSONSerialization.data(withJSONObject: metadata, options: .sortedKeys), encoding: .utf8)!)
guard mode != "inspect" else { exit(0) }
func captureOwnedWindow(_ state: String) {
    foreground()
    guard let pointer = CGEvent(source: nil)?.location, hypot(pointer.x-destination.x,pointer.y-destination.y)<1 else { fail("Owned pointer moved; capture evidence rejected (observed=\(String(describing: CGEvent(source:nil)?.location)), expected=\(destination))") }
    guard CGPreflightScreenCaptureAccess() else { fail("Existing screen capture permission unavailable; no permission settings changed") }
    var content: SCShareableContent?, failure: Error?, complete = false
    SCShareableContent.getExcludingDesktopWindows(true, onScreenWindowsOnly: true) { value, error in
        content = value; failure = error; complete = true
    }
    let deadline = Date(timeIntervalSinceNow: 5)
    while !complete && Date() < deadline { RunLoop.current.run(until: Date(timeIntervalSinceNow: 0.01)) }
    guard complete, failure == nil, let content else { fail("Owned capture inventory failed") }
    let candidates = content.windows.filter { candidate in
        candidate.owningApplication?.processID == application.processIdentifier &&
        (target == "source" ? (candidate.title ?? "").hasPrefix("NumberInput · original isolation") : candidate.title == "Replica NumberInput verification") &&
        abs(candidate.frame.minX - windowRect.minX) < 1 && abs(candidate.frame.minY - windowRect.minY) < 1
    }
    guard candidates.count == 1 else { fail("Owned capture window must be unique") }
    let filter = SCContentFilter(desktopIndependentWindow: candidates[0])
    let configuration = SCStreamConfiguration()
    configuration.width = Int(windowRect.width * 2); configuration.height = Int(windowRect.height * 2)
    configuration.showsCursor = false; configuration.ignoreShadowsSingleWindow = true
    configuration.colorSpaceName = CGColorSpace.sRGB
    var image: CGImage?, captureError: Error?, captured = false
    SCScreenshotManager.captureImage(contentFilter: filter, configuration: configuration) { value, error in
        image = value; captureError = error; captured = true
    }
    let imageDeadline = Date(timeIntervalSinceNow: 5)
    while !captured && Date() < imageDeadline { RunLoop.current.run(until: Date(timeIntervalSinceNow: 0.01)) }
    guard captured, captureError == nil, let image else { fail("Owned window frame capture failed") }
    foreground()
    guard let pointer = CGEvent(source: nil)?.location, hypot(pointer.x-destination.x,pointer.y-destination.y)<1 else { fail("Owned pointer moved during capture; evidence rejected") }
    let capturedValue=attribute(field,kAXValueAttribute)
    let capturedNumber=(capturedValue as? NSNumber)?.doubleValue ?? Double(capturedValue as? String ?? "")
    let expectedNumber=state == "pressed" ? (half == "up" ? 3307.0 : 3305.0) : 3306.0
    guard capturedNumber == expectedNumber else { fail("Owned state did not reach expected sample value; evidence rejected") }
    let bitmap = NSBitmapImageRep(cgImage: image)
    guard let png = bitmap.representation(using: .png, properties: [:]) else { fail("Owned PNG encoding failed") }
    let prefix = "/Users/youptebook/workspace/DB-Migration-Verifier/artifacts/replica/mac-\(target)-\(half)-\(state)-official"
    do {
        try png.write(to: URL(fileURLWithPath: prefix + ".png"))
        try JSONSerialization.data(withJSONObject: metadata, options: .sortedKeys).write(to: URL(fileURLWithPath: prefix + ".json"))
    } catch { fail("Owned capture output failed") }
}
application.activate(options: [])
AXUIElementPerformAction(window, kAXRaiseAction as CFString)
RunLoop.current.run(until: Date(timeIntervalSinceNow: 0.25))
func foreground() {
    guard NSWorkspace.shared.frontmostApplication?.processIdentifier == application.processIdentifier else { fail("Owned fixture must be foreground; input blocked (observed PID=\(NSWorkspace.shared.frontmostApplication?.processIdentifier ?? -1), expected PID=\(application.processIdentifier))") }
}
foreground()
let value = attribute(field, kAXValueAttribute)
let numericValue = (value as? NSNumber)?.doubleValue ?? Double(value as? String ?? "")
guard numericValue == 3306 else { fail("Expected synthetic start value 3306; input blocked") }
let destination = CGPoint(x: rect.maxX - 18.5, y: rect.minY + (half == "up" ? 16.5 : 27.5))
let previous = CGEvent(source: nil)?.location
func send(_ type: CGEventType, _ point: CGPoint) {
    guard let event = CGEvent(mouseEventSource: nil, mouseType: type, mouseCursorPosition: point, mouseButton: .left) else { fail("CGEvent creation failed") }
    event.post(tap: .cghidEventTap)
}
var pressed = false
defer {
    if pressed { send(.leftMouseUp, destination) }
    if NSWorkspace.shared.frontmostApplication?.processIdentifier == application.processIdentifier, let previous { send(.mouseMoved, previous) }
}
send(.mouseMoved, destination)
Thread.sleep(forTimeInterval: 0.35)
foreground()
if mode == "hover" { captureOwnedWindow("hover") }
else {
    send(.leftMouseDown, destination); pressed = true; releasePoint = destination
    if mode == "pressed" {
        Thread.sleep(forTimeInterval: 0.15); captureOwnedWindow("pressed")
    } else { Thread.sleep(forTimeInterval: mode == "hold" ? 1.5 : 0.035) }
    send(.leftMouseUp, destination); pressed = false; releasePoint = nil
    Thread.sleep(forTimeInterval: 0.35)
}
