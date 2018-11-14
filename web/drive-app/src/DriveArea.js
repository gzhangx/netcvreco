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
        me.sendCmd(x,y);
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
        }).catch(exc=>{
            me.setState({ x:xx, y:yy });
        });
        //this.setState({displayX: x, displayY:y});
    }

    render() {
        const { x, y, displayX, displayY } = this.state;
        return <div ref="drawArea" onMouseMove={this._onMouseMove.bind(this)} className="App-work-area">
            <p>Mouse coordinates: { x },{ y } ({ displayX}, {displayY})</p>
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