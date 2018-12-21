import React, { Component } from 'react';
import './App.css';
import api from './api';
import debounce from 'lodash/debounce';

class DriveArea extends Component{
    constructor(props) {
        super(props);

        this.state = { x: 100, y: 0 };
        this.sendCmd = this.sendCmd.bind(this);
    }

    _onMouseMove(e) {

        //200->180
        //const boundingRect = this.refs.drawArea.getBoundingClientRect(); //left top
        //e.screenX - boundingRect.left, y: e.screenY - boundingRect.top
        let x = e.nativeEvent.offsetX;
        let y = e.nativeEvent.offsetY;
        if (x > 200) x = 200;
        if (x < 0) x = 0;
        const me = this;
        //debounce(()=>{
            //me.setState({ x, y });
        //me.sendCmd(x,y);
        //}, 100)();
    }

    sendCmd(xx,yy) {
        const me = this;
        const y = parseInt((100-yy)/20);
        const x = parseInt(((xx-100)/10)+90);
        api.cmdReq(`api/r/${x}`).then(()=>{
          return api.cmdReq(`api/d/${y}`);
        }).then(()=>{
            me.setState({ x:xx, y:yy });
        }).catch(e=>{
            console.log(e);
            me.setState({ error:e });
        });
        //this.setState({displayX: x, displayY:y});
    }

    goleft() {
        const me = this;
        api.cmdReq(`api/r/0`).then(()=>{
            me.setState({ x:0 });
        }).catch(e=>{
            console.log(e);
            me.setState({ error:e });
        });
    }
    goright() {
        const me = this;
        api.cmdReq(`api/r/180`).then(()=>{
            me.setState({ x:180 });
        }).catch(e=>{
            console.log(e);
            me.setState({ error:e });
        })
    }
    forward() {
        const me = this;
        api.cmdReq(`api/r/90`).then(()=>{
            api.cmdReq(`api/d/5`);
            me.setState({ x:100, y: 0 });
        }).catch(e=>{
            console.log(e);
            me.setState({ error:e });
        });
    }

    stop() {
        const me = this;
        api.cmdReq(`api/d/0`).then(()=>{
            me.setState({ x:100, y:200 });
        });
    }

    replay() {
        return api.cmdReq(`api/replay`)
    }
    cancelReplay() {
        return api.cmdReq(`api/cancelReplay`)
    }
    render() {
        const { x, y, displayX, displayY } = this.state;
        return <div ref="drawArea" onMouseMove={this._onMouseMove.bind(this)} className="App-work-area">
            <p>Mouse coordinates: { x },{ y } ({ displayX}, {displayY})</p>
            <button onClick={()=>this.goleft()}>Left</button>
            <button onClick={()=>this.forward()}>Forward</button>
            <button onClick={()=>this.goright()}>Right</button>
            <div>
                <button onClick={()=>this.stop()}>Stop</button>
            </div>
            <div>
                <button onClick={()=>this.replay()}>Replay</button>
                <button onClick={()=>this.cancelReplay()}>Cancel</button>
            </div>
            <svg>
                <line
                    x1={100}
                    y1={200}
                    x2={x}
                    y2={y}
                    style={{
                        stroke: '#458232',
                        strokeWidth: '3px',
                    }}
                />
            </svg>
        </div>;
    }
}
export default DriveArea;