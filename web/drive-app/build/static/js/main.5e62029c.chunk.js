(window.webpackJsonp=window.webpackJsonp||[]).push([[0],{16:function(e,t,n){e.exports=n(38)},21:function(e,t,n){},38:function(e,t,n){"use strict";n.r(t);var a=n(0),o=n.n(a),r=n(13),c=n.n(r),s=(n(21),n(2)),i=n(3),l=n(5),u=n(4),d=n(6),p=(n(8),n(1)),m=n(14),v=n.n(m),h=function(e){return v.a.get("/".concat(e)).send().then(function(e){console.log("request returns"),console.log(e)})},f=n(15),y=n.n(f),b=function(e){function t(e){var n;return Object(s.a)(this,t),(n=Object(l.a)(this,Object(u.a)(t).call(this,e))).state={x:100,y:0},n.sendCmd=n.sendCmd.bind(Object(p.a)(Object(p.a)(n))),n}return Object(d.a)(t,e),Object(i.a)(t,[{key:"_onMouseMove",value:function(e){var t=e.nativeEvent.offsetX,n=e.nativeEvent.offsetY,a=this;y()(function(){a.setState({x:t,y:n}),a.sendCmd(t,n)},100)()}},{key:"sendCmd",value:function(e,t){var n=parseInt((100-t)/20),a=e/200*20+90;h("api/r/".concat(a)),h("api/d/".concat(n)),this.setState({displayX:a,displayY:n})}},{key:"render",value:function(){var e=this.state,t=e.x,n=e.y,a=e.displayX,r=e.displayY;return o.a.createElement("div",{ref:"drawArea",onMouseMove:this._onMouseMove.bind(this),className:"App-work-area"},o.a.createElement("p",null,"Mouse coordinates: ",t,",",n," (",a,", ",r,")"),o.a.createElement("svg",null,o.a.createElement("line",{x1:100,y1:200,x2:t,y2:n,style:{stroke:"#458232",strokeWidth:"3px"}})))}}]),t}(a.Component),w=function(e){function t(){return Object(s.a)(this,t),Object(l.a)(this,Object(u.a)(t).apply(this,arguments))}return Object(d.a)(t,e),Object(i.a)(t,[{key:"render",value:function(){return o.a.createElement("div",{className:"App"},o.a.createElement("header",{className:"App-header"},o.a.createElement(b,null)))}}]),t}(a.Component);Boolean("localhost"===window.location.hostname||"[::1]"===window.location.hostname||window.location.hostname.match(/^127(?:\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3}$/));c.a.render(o.a.createElement(w,null),document.getElementById("root")),"serviceWorker"in navigator&&navigator.serviceWorker.ready.then(function(e){e.unregister()})},8:function(e,t,n){}},[[16,2,1]]]);
//# sourceMappingURL=main.5e62029c.chunk.js.map